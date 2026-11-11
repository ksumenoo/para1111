using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        public bool TryAttack(BodyState body, Vector3 direction)
        {
            if (Phase != RunPhase.Fighting || !body.Alive || body.AttackCooldown > 0 || body.Windup > 0) return false;
            body.AttackDirection = Flat(direction).sqrMagnitude > .001f ? Flat(direction).normalized : body.Facing;
            body.Windup = Mathf.Max(.001f, body.IsPlayer ? body.Definition.PlayerWindup : body.Definition.EnemyWindup);
            body.AttackCooldown = body.Definition.AttackCooldown + body.Windup;
            Emit(CombatCue.Windup, body, body.AttackDirection);
            return true;
        }

        private void ResolveAttack(BodyState attacker)
        {
            if (!attacker.Alive || Phase != RunPhase.Fighting) return;
            Emit(CombatCue.Attack, attacker, attacker.AttackDirection);
            float damage = attacker.Definition.Damage * (attacker.IsPlayer ? 1 : settings.EnemyDamageMultiplier);
            if (attacker.Definition.Kind == BodyKind.Gunner)
            {
                shots.Add(new ShotState { Source = attacker, PlayerShot = attacker.IsPlayer, Damage = damage, Position = attacker.Position + attacker.AttackDirection * .8f, Direction = attacker.AttackDirection });
                return;
            }
            float cosine = Mathf.Cos(attacker.Definition.Arc * .5f * Mathf.Deg2Rad);
            for (int i = 0; i < bodies.Count; i++)
            {
                var target = bodies[i];
                if (!target.Alive || target == attacker || attacker.IsPlayer == target.IsPlayer) continue;
                Vector3 offset = target.Position - attacker.Position;
                if (offset.magnitude <= attacker.Definition.Range + target.Definition.Radius && (offset.sqrMagnitude < .01f || Vector3.Dot(offset.normalized, attacker.AttackDirection) >= cosine))
                    Damage(target, damage, attacker, attacker.AttackDirection);
                if (Phase != RunPhase.Fighting) break;
            }
        }

        public void Damage(BodyState target, float amount, BodyState source = null, Vector3 direction = default)
        { ApplyDamage(target, amount, source, direction, source != null && source.IsPlayer); }

        private void ApplyDamage(BodyState target, float amount, BodyState source, Vector3 direction, bool playerDamage)
        {
            if (target == null || !target.Alive || target.Invulnerable > 0 || amount <= 0 || Phase != RunPhase.Fighting) return;
            if (source != null && playerDamage == target.IsPlayer) return;
            target.Health = Mathf.Max(0, target.Health - amount);
            if (playerDamage && !target.IsPlayer) target.MarkUntil = worldTime + settings.MarkDuration;
            if (target.IsPlayer) target.Invulnerable = .3f;
            target.Knockback = Flat(direction).normalized * (target.Definition.Kind == BodyKind.Brute ? 3 : 6);
            Emit(CombatCue.Hit, target, direction, source, amount);
            stopRemaining = Mathf.Max(stopRemaining, settings.HitStop);
            if (target.Alive) return;
            target.Windup = 0;
            Emit(CombatCue.Death, target, direction, source);
            if (target.IsPlayer) BeginPossession(target.Position);
            else Kills++;
        }

        private void TickShots(float delta)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                Vector3 previous = shot.Position;
                shot.Position += shot.Direction * settings.ProjectileSpeed * delta;
                shot.Remaining -= delta;
                bool hit = false;
                for (int j = 0; j < bodies.Count; j++)
                {
                    var target = bodies[j];
                    if (!target.Alive || target == shot.Source || target.IsPlayer == shot.PlayerShot) continue;
                    var line = shot.Position - previous;
                    float fraction = line.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(target.Position - previous, line) / line.sqrMagnitude) : 0;
                    if ((previous + line * fraction - target.Position).sqrMagnitude > Mathf.Pow(target.Definition.Radius + .15f, 2)) continue;
                    // Allegiance belongs to the shot, even if its source has since been possessed.
                    DamageFromShot(target, shot);
                    hit = true; break;
                }
                if (hit || shot.Remaining <= 0 || Mathf.Abs(shot.Position.x) > settings.ArenaHalfSize.x || Mathf.Abs(shot.Position.z) > settings.ArenaHalfSize.y) shots.RemoveAt(i);
            }
        }

        private void DamageFromShot(BodyState target, ShotState shot)
        {
            ApplyDamage(target, shot.Damage, shot.Source, shot.Direction, shot.PlayerShot);
        }
    }
}
