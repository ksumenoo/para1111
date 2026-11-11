using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondChance
{
    // Owns gameplay state; views and input never write body health or ownership.
    public sealed partial class GameSession
    {
        private readonly GameSettings settings;
        private readonly List<BodyState> bodies = new List<BodyState>(32);
        private readonly List<ShotState> shots = new List<ShotState>(64);
        private readonly System.Random random = new System.Random();
        private int nextId;
        private float worldTime, phaseTime, stopRemaining;
        private Vector3 soulOrigin;
        public GameSettings Settings => settings;
        public IReadOnlyList<BodyState> Bodies => bodies;
        public IReadOnlyList<ShotState> Shots => shots;
        public BodyState Player { get; private set; }
        public BodyState Candidate { get; private set; }
        public RunPhase Phase { get; private set; } = RunPhase.Ready;
        public float Elapsed { get; private set; }
        public float PossessionRemaining { get; private set; }
        public int Wave { get; private set; }
        public int Transfers { get; private set; }
        public int Kills { get; private set; }
        public float WorldScale => Phase == RunPhase.Possessing ? settings.DeathSlowMotion : stopRemaining > 0 ? 0 : 1;
        public float DashReady => Player == null ? 0 : Mathf.Clamp01(1 - Player.DashCooldown / settings.DashCooldown);
        public event Action<CombatEvent> Combat;
        public event Action<RunPhase> PhaseChanged;
        public event Action<BodyState> Spawned;

        public GameSession(GameSettings settings) { this.settings = settings; }

        public void StartRun()
        {
            bodies.Clear(); shots.Clear(); nextId = 0; worldTime = 0; phaseTime = 0; stopRemaining = 0;
            Elapsed = 0; Wave = 0; Transfers = 0; Kills = 0; Candidate = null; PossessionRemaining = 0;
            Player = Spawn(BodyKind.Duelist, Vector3.zero, true);
            Player.Invulnerable = 1;
            StartWave();
        }

        public BodyState Spawn(BodyKind kind, Vector3 position, bool player = false)
        {
            var definition = settings.GetBody(kind);
            var body = new BodyState { Id = ++nextId, Definition = definition, Health = definition.Health, Position = Clamp(position, definition.Radius), IsPlayer = player };
            bodies.Add(body);
            Spawned?.Invoke(body);
            return body;
        }

        public bool IsMarked(BodyState body) => body != null && body.Alive && !body.IsPlayer && body.MarkUntil > worldTime;
        public float MarkRemaining01(BodyState body) => IsMarked(body) ? Mathf.Clamp01((body.MarkUntil - worldTime) / Mathf.Max(.01f, settings.MarkDuration)) : 0;
        public bool CanPossess(BodyState body) => Phase == RunPhase.Possessing && IsMarked(body) && (body.Position - soulOrigin).sqrMagnitude <= settings.PossessionRadius * settings.PossessionRadius;

        public void Tick(float realDelta, PlayerCommand command)
        {
            if (Phase == RunPhase.Ready || Phase == RunPhase.Won || Phase == RunPhase.Lost) return;
            // Countdown deliberately uses realDelta, independently of slow motion or hit stop.
            if (Phase == RunPhase.Possessing)
            {
                PossessionRemaining -= realDelta;
                FindCandidate();
                if (PossessionRemaining <= 0) { SetPhase(RunPhase.Lost); return; }
                if (command.Possess) TryPossess();
            }
            stopRemaining = Mathf.Max(0, stopRemaining - realDelta);
            float delta = realDelta * WorldScale;
            worldTime += delta;
            if (Phase == RunPhase.Fighting || Phase == RunPhase.Intermission) Elapsed += delta;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (!body.Alive) continue;
                body.Invulnerable = Mathf.Max(0, body.Invulnerable - delta);
                body.AttackCooldown = Mathf.Max(0, body.AttackCooldown - delta);
                body.DashCooldown = Mathf.Max(0, body.DashCooldown - delta);
                if (body.Windup > 0)
                {
                    body.Windup -= delta;
                    if (body.Windup <= 0) ResolveAttack(body);
                }
                if (!body.Alive) continue;
                if (body.IsPlayer && Phase != RunPhase.Possessing) TickPlayer(body, command, delta);
                else TickEnemy(body, delta);
                body.Position = Clamp(body.Position + body.Knockback * delta, body.Definition.Radius);
                body.Knockback = Vector3.MoveTowards(body.Knockback, Vector3.zero, delta * 35);
            }
            SeparateBodies();
            TickShots(delta);
            if (Phase == RunPhase.Fighting || Phase == RunPhase.Intermission) TickWaves(delta);
            // Retain dead bodies for one frame so presentation can process death events, then prune.
            for (int i = bodies.Count - 1; i >= 0; i--) if (!bodies[i].Alive && bodies[i] != Player) bodies.RemoveAt(i);
        }

        private void TickPlayer(BodyState body, PlayerCommand command, float delta)
        {
            var move = Vector3.ClampMagnitude(Flat(command.Move), 1);
            var aim = Flat(command.Aim - body.Position);
            if (aim.sqrMagnitude > .01f) body.Facing = aim.normalized;
            if (command.Dash && body.DashCooldown <= 0)
            {
                body.DashDirection = move.sqrMagnitude > .01f ? move.normalized : body.Facing;
                body.DashTime = settings.DashDuration; body.DashCooldown = settings.DashCooldown;
                body.Invulnerable = Mathf.Max(body.Invulnerable, settings.DashDuration);
                body.Windup = 0;
                Emit(CombatCue.Dash, body, body.DashDirection);
            }
            if (body.DashTime > 0)
            {
                float step = Mathf.Min(delta, body.DashTime);
                body.DashTime -= step;
                body.Position = Clamp(body.Position + body.DashDirection * settings.DashSpeed * step, body.Definition.Radius);
            }
            else
            {
                body.Position = Clamp(body.Position + move * body.Definition.Speed * delta * (body.Windup > 0 ? .5f : 1), body.Definition.Radius);
                if (command.Attack) TryAttack(body, body.Facing);
            }
        }

        private void TickEnemy(BodyState body, float delta)
        {
            if (Player == null || (Phase != RunPhase.Fighting && Phase != RunPhase.Possessing)) return;
            Vector3 offset = (Phase == RunPhase.Possessing ? soulOrigin : Player.Position) - body.Position;
            float distance = offset.magnitude;
            Vector3 direction = distance > .001f ? offset / distance : body.Facing;
            if (body.Windup > 0) return;
            body.Facing = direction;
            float speed = body.Definition.Speed * .68f;
            if (body.Definition.Kind == BodyKind.Gunner)
            {
                if (distance < 5) body.Position = Clamp(body.Position - direction * speed * delta, body.Definition.Radius);
                else if (distance > 9) body.Position = Clamp(body.Position + direction * speed * delta, body.Definition.Radius);
            }
            else if (distance > body.Definition.Range * .7f)
                body.Position = Clamp(body.Position + direction * speed * delta, body.Definition.Radius);
            if (Phase == RunPhase.Fighting && distance <= body.Definition.Range * .9f) TryAttack(body, direction);
        }

        private void SeparateBodies()
        {
            for (int i = 0; i < bodies.Count; i++)
            for (int j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i]; var b = bodies[j];
                if (!a.Alive || !b.Alive || a.DashTime > 0 || b.DashTime > 0) continue;
                Vector3 difference = a.Position - b.Position;
                float minimum = a.Definition.Radius + b.Definition.Radius;
                float distance = difference.magnitude;
                if (distance >= minimum) continue;
                var push = (distance > .001f ? difference / distance : Vector3.right) * (minimum - distance) * .5f;
                a.Position = Clamp(a.Position + push, a.Definition.Radius);
                b.Position = Clamp(b.Position - push, b.Definition.Radius);
            }
        }

        private Vector3 Clamp(Vector3 value, float radius) => new Vector3(Mathf.Clamp(value.x, -settings.ArenaHalfSize.x + radius, settings.ArenaHalfSize.x - radius), 0, Mathf.Clamp(value.z, -settings.ArenaHalfSize.y + radius, settings.ArenaHalfSize.y - radius));
        private static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0, value.z);
        private void SetPhase(RunPhase phase) { Phase = phase; phaseTime = 0; PhaseChanged?.Invoke(phase); }
        private void Emit(CombatCue cue, BodyState body, Vector3 direction, BodyState other = null, float amount = 0) => Combat?.Invoke(new CombatEvent(cue, body, direction, other, amount));
    }
}
