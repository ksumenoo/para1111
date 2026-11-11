using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        public bool TryAttack(BodyState body, Vector3 direction)
        {
            if (Phase != RunPhase.Fighting || !body.Alive || body.AttackCooldown > 0 || body.Windup > 0) return false;
            body.AttackDirection = Flat(direction).sqrMagnitude > .001f ? Flat(direction).normalized : body.Facing;
            body.Windup = Mathf.Max(.001f, body.IsPlayer ? body.Definition.PlayerWindup : body.Definition.EnemyWindup * (body.IsBoss ? .85f : 1));
            body.AttackCooldown = body.Definition.AttackCooldown + body.Windup;
            Emit(CombatCue.Windup, body, body.AttackDirection);
            return true;
        }

        private void ResolveAttack(BodyState attacker)
        {
            if (!attacker.Alive || Phase != RunPhase.Fighting) return;
            Emit(CombatCue.Attack, attacker, attacker.AttackDirection);
            float damage = attacker.Definition.Damage * (attacker.IsPlayer ? (Surging ? settings.SurgeDamage : 1) : settings.EnemyDamageMultiplier * (attacker.IsBoss ? settings.BossDamageScale : 1));
            if (attacker.Definition.Kind == BodyKind.Gunner)
            {
                if(Layout.CastFraction(attacker.Position,attacker.Position+attacker.AttackDirection*.8f,.1f)<1)return;
                // The boss reuses the pistol as a three-round fan.
                int spread = attacker.IsBoss ? 1 : 0;
                for (int i = -spread; i <= spread; i++)
                {
                    var direction = Quaternion.AngleAxis(i * 13, Vector3.up) * attacker.AttackDirection;
                    shots.Add(new ShotState { Source = attacker, PlayerShot = attacker.IsPlayer, Damage = damage, Position = attacker.Position + attacker.AttackDirection * .8f, Direction = direction });
                }
                return;
            }
            float cosine = Mathf.Cos(attacker.Definition.Arc * .5f * Mathf.Deg2Rad);
            for (int i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                if (!prop.Alive) continue;
                Vector3 offset = prop.Position - attacker.Position;
                if (offset.magnitude <= attacker.Definition.Range + prop.Radius && (offset.sqrMagnitude < .01f || Vector3.Dot(offset.normalized, attacker.AttackDirection) >= cosine) && Layout.HasSight(attacker.Position,prop.Position))
                    DamageProp(prop, damage, attacker.AttackDirection);
                if (Phase != RunPhase.Fighting) return;
            }
            for (int i = 0; i < bodies.Count; i++)
            {
                var target = bodies[i];
                if (!target.Alive || target == attacker || attacker.IsPlayer == target.IsPlayer) continue;
                Vector3 offset = target.Position - attacker.Position;
                if (Layout.HasSight(attacker.Position,target.Position) && offset.magnitude <= attacker.Definition.Range + target.Definition.Radius && (offset.sqrMagnitude < .01f || Vector3.Dot(offset.normalized, attacker.AttackDirection) >= cosine))
                    Damage(target, damage, attacker, attacker.AttackDirection);
                if (Phase != RunPhase.Fighting) break;
            }
        }

        public void Damage(BodyState target, float amount, BodyState source = null, Vector3 direction = default)
        { ApplyDamage(target, amount, source, direction, source != null && source.IsPlayer); }

        private void ApplyDamage(BodyState target, float amount, BodyState source, Vector3 direction, bool playerDamage, float push = -1)
        {
            if (target == null || !target.Alive || target.Invulnerable > 0 || amount <= 0 || Phase != RunPhase.Fighting) return;
            if (source != null && playerDamage == target.IsPlayer) return;
            target.Health = Mathf.Max(0, target.Health - amount);
            if (playerDamage && !target.IsPlayer) target.MarkUntil = worldTime + settings.MarkDuration;
            if (target.IsPlayer) target.Invulnerable = .3f;
            bool sturdy = target.Definition.Unstoppable || target.IsBoss;
            if (push < 0) push = source != null ? source.Definition.Push : 6;
            target.Knockback = Flat(direction).normalized * push * (sturdy ? .45f : 1);
            // Ordinary enemies flinch and lose their attack; the Brute and the boss swing through.
            if (playerDamage && !target.IsPlayer && !sturdy && target.Alive)
            {
                target.Stagger = settings.HitStagger;
                if (target.Windup > 0) { target.Windup = 0; target.AttackCooldown = Mathf.Max(target.AttackCooldown, .35f); }
            }
            Emit(CombatCue.Hit, target, direction, source, amount);
            bool heavy = !target.Alive || (source != null && source.Definition.Kind == BodyKind.Brute) || source == null;
            stopRemaining = Mathf.Max(stopRemaining, heavy ? settings.HeavyHitStop : settings.HitStop);
            if (target.Alive) return;
            target.Windup = 0; target.Stagger = 0;
            Emit(CombatCue.Death, target, direction, source);
            if (target.IsPlayer) { BeginPossession(target.Position); return; }
            Kills++;
            if (target.IsBoss) { target.IsBoss = false; QueueBossShift(target.Position); }
            if (!playerDamage || Player == null || !Player.Alive) return;
            if (Surging) surgeUntil = Mathf.Min(worldTime + settings.SurgeDuration, surgeUntil + settings.SurgeKillExtension);
            else if (chainUntil > worldTime) StartSurge();
        }

        private void DamageProp(PropState prop, float amount, Vector3 direction)
        {
            if (prop == null || !prop.Alive || amount <= 0) return;
            prop.Health = Mathf.Max(0, prop.Health - amount);
            stopRemaining = Mathf.Max(stopRemaining, prop.Alive ? settings.HitStop * .5f : settings.HitStop);
            PropHit?.Invoke(prop, direction);
            if (prop.Alive || !prop.Explosive) return;
            // Enemies first: the blast marks them before it can drop the player into the death window.
            float radius = settings.ExplosionRadius;
            for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (!body.Alive || body.IsPlayer != (pass == 1)) continue;
                Vector3 offset = body.Position - prop.Position;
                if (offset.magnitude > radius + body.Definition.Radius || !Layout.HasSight(prop.Position,body.Position)) continue;
                ApplyDamage(body, body.IsPlayer ? settings.ExplosionSelfDamage : settings.ExplosionDamage, null, offset, !body.IsPlayer, 13);
            }
            for (int i = 0; i < props.Count; i++)
                if (props[i].Alive && (props[i].Position - prop.Position).magnitude <= radius) DamageProp(props[i], settings.ExplosionDamage, props[i].Position - prop.Position);
        }

        private void TickShots(float delta)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                Vector3 previous = shot.Position;
                shot.Position += shot.Direction * settings.ProjectileSpeed * delta;
                float wallFraction=Layout.CastFraction(previous,shot.Position,.1f);
                bool wallHit=wallFraction<1;
                shot.Position=Vector3.Lerp(previous,shot.Position,wallFraction);
                shot.Remaining -= delta;
                bool hit = false;
                BodyState nearest=null; PropState nearestProp=null; float nearestFraction=float.MaxValue;
                var line = shot.Position - previous;
                for (int j = 0; j < bodies.Count; j++)
                {
                    var target = bodies[j];
                    if (!target.Alive || target == shot.Source || target.IsPlayer == shot.PlayerShot) continue;
                    float fraction = line.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(target.Position - previous, line) / line.sqrMagnitude) : 0;
                    if ((previous + line * fraction - target.Position).sqrMagnitude > Mathf.Pow(target.Definition.Radius + .15f, 2)) continue;
                    if(fraction<nearestFraction) { nearest=target; nearestFraction=fraction; }
                }
                for (int j = 0; j < props.Count; j++)
                {
                    var prop = props[j];
                    if (!prop.Alive) continue;
                    float fraction = line.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(prop.Position - previous, line) / line.sqrMagnitude) : 0;
                    if ((previous + line * fraction - prop.Position).sqrMagnitude > Mathf.Pow(prop.Radius + .1f, 2)) continue;
                    if(fraction<nearestFraction) { nearest=null; nearestProp=prop; nearestFraction=fraction; }
                }
                if(nearestProp!=null) { DamageProp(nearestProp,shot.Damage,shot.Direction); hit=true; }
                else if(nearest!=null) { DamageFromShot(nearest,shot); hit=true; }
                if (hit || wallHit || shot.Remaining <= 0 || Mathf.Abs(shot.Position.x) > Layout.HalfSize.x || Mathf.Abs(shot.Position.z) > Layout.HalfSize.y) shots.RemoveAt(i);
            }
        }

        private void DamageFromShot(BodyState target, ShotState shot)
        {
            ApplyDamage(target, shot.Damage, shot.Source, shot.Direction, shot.PlayerShot);
        }
    }
}
