using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        private void BeginPossession(Vector3 position)
        {
            soulOrigin = position;
            PossessionRemaining = settings.PossessionSeconds;
            stopRemaining = 0; surgeUntil = 0; chainUntil = 0;
            SetPhase(RunPhase.Possessing);
            FindCandidate();
        }

        private void FindCandidate()
        {
            Candidate = null;
            float nearest = float.MaxValue;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (!CanPossess(body)) continue;
                float distance = (body.Position - soulOrigin).sqrMagnitude;
                if (distance < nearest) { nearest = distance; Candidate = body; }
            }
        }

        public bool TryPossess()
        {
            if (Phase != RunPhase.Possessing || PossessionRemaining <= 0) return false;
            FindCandidate();
            if (Candidate == null) return false;
            var oldBody = Player;
            oldBody.IsPlayer = false;
            Player = Candidate;
            Player.IsPlayer = true;
            Player.MarkUntil = 0; Player.Windup = 0; Player.AttackCooldown = 0; Player.Stagger = 0;
            Player.DashCooldown = 0; Player.DashTime = 0; Player.Knockback = Vector3.zero;
            Player.Invulnerable = settings.PossessionGrace;
            Candidate = null; Transfers++; PossessionRemaining = 0;
            // Taking the boss's own host throws his soul out: it costs him that body.
            bool evictedBoss = Player.IsBoss;
            Player.IsBoss = false;
            chainUntil = worldTime + settings.ChainWindow;
            SetPhase(RunPhase.Fighting);
            Emit(CombatCue.Possessed, Player, Player.Position - soulOrigin, oldBody);
            if (evictedBoss) QueueBossShift(Player.Position);
            return true;
        }
    }
}
