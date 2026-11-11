using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        // The boss is a rival soul: each time its host dies it takes another body, a different kind every time.
        private static readonly BodyKind[] BossForms = { BodyKind.Brute, BodyKind.Duelist, BodyKind.Gunner };
        private bool bossShiftQueued;
        private Vector3 bossShiftOrigin;
        private int bossForm;
        public BodyState Boss { get; private set; }
        public int BossLivesLeft { get; private set; }
        public bool BossPending { get; private set; }
        public bool BossFight { get; private set; }
        public int BossStage => BossFight ? Mathf.Clamp(settings.BossLives - BossLivesLeft + 1, 1, settings.BossLives) : 0;

        private void ResetBoss()
        { Boss = null; BossLivesLeft = 0; BossPending = false; BossFight = false; bossShiftQueued = false; bossForm = 0; }

        private void StartWave()
        {
            Wave++;
            WavesThisRoom++;
            var authored = settings.UseRooms && settings.Waves != null && Wave <= settings.Waves.Length ? settings.Waves[Wave - 1] : null;
            if (authored != null)
            {
                // Melee first, so the early spawn points are not all ranged.
                int index = 0;
                for (int i = 0; i < authored.Duelists && index<settings.MaxWaveSize; i++) SpawnEnemy(BodyKind.Duelist, index++);
                for (int i = 0; i < authored.Brutes && index<settings.MaxWaveSize; i++) SpawnEnemy(BodyKind.Brute, index++);
                for (int i = 0; i < authored.Gunners && index<settings.MaxWaveSize; i++) SpawnEnemy(BodyKind.Gunner, index++);
            }
            else
            {
                int count = Mathf.Min(settings.MaxWaveSize, settings.FirstWaveSize + (Wave - 1) * settings.WaveGrowth);
                for (int i = 0; i < count; i++)
                {
                    var kind = Wave == 1 ? BodyKind.Duelist : (BodyKind)random.Next(3);
                    // Each wave after the first exposes all three body options.
                    if (Wave > 1 && i < 3) kind = (BodyKind)i;
                    SpawnEnemy(kind, i);
                }
            }
            SetPhase(RunPhase.Fighting);
        }

        private BodyState SpawnEnemy(BodyKind kind, int i)
        {
            var half = Layout.HalfSize;
            float x = (float)(random.NextDouble() * 2 - 1) * (half.x - 1);
            float z = (float)(random.NextDouble() * 2 - 1) * (half.y - 1);
            if (random.Next(2) == 0) x = (i % 2 == 0 ? -1 : 1) * (half.x - 1);
            else z = (i % 2 == 0 ? -1 : 1) * (half.y - 1);
            var position = new Vector3(x, 0, z);
            if(settings.UseRooms && Layout.Spawns.Count>0)
            {
                // Prefer a spawn point that is not on top of the player.
                for(int attempt=0;attempt<6;attempt++)
                {
                    position=Layout.Spawns[random.Next(Layout.Spawns.Count)]+new Vector3((float)random.NextDouble()-.5f,0,(float)random.NextDouble()-.5f);
                    if(Player==null || (position-Player.Position).sqrMagnitude>=36)break;
                }
            }
            else if (Player != null && (position - Player.Position).sqrMagnitude < 25) position = -position;
            var body = Spawn(kind, position);
            body.AttackCooldown = 1;
            return body;
        }

        private void StartBoss()
        {
            BossPending = false; BossFight = true; bossForm = 0;
            BossLivesLeft = settings.BossLives;
            var host = SpawnEnemy(BossForms[0], 0);
            for (int i = 0; i < settings.BossEscorts; i++) SpawnEnemy(i % 2 == 0 ? BodyKind.Duelist : BodyKind.Gunner, i + 1);
            SetPhase(RunPhase.Fighting);
            MakeBoss(host, host.Position);
        }

        private void MakeBoss(BodyState host, Vector3 from)
        {
            host.IsBoss = true;
            host.MaxHealth = host.Definition.Health * settings.BossHealthScale;
            host.Health = host.MaxHealth;
            host.MarkUntil = 0; host.Stagger = 0; host.Windup = 0; host.Knockback = Vector3.zero;
            host.Invulnerable = 1; host.AttackCooldown = 1.2f;
            Boss = host;
            Emit(CombatCue.BossShift, host, host.Position - from, null, BossLivesLeft);
        }

        private void QueueBossShift(Vector3 from)
        { Boss = null; bossShiftQueued = true; bossShiftOrigin = from; }

        private void ResolveBossShift()
        {
            bossShiftQueued = false;
            BossLivesLeft--;
            if (BossLivesLeft <= 0) { BossFight = false; shots.Clear(); SetPhase(RunPhase.Won); return; }
            bossForm++;
            var wanted = BossForms[bossForm % BossForms.Length];
            // Nearest living henchman of the wanted kind; otherwise a fresh body walks in.
            BodyState host = null; float nearest = float.MaxValue; int living = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (!body.Alive || body.IsPlayer) continue;
                living++;
                if (body.Definition.Kind != wanted) continue;
                float distance = (body.Position - bossShiftOrigin).sqrMagnitude;
                if (distance < nearest) { nearest = distance; host = body; }
            }
            if (host == null) { host = SpawnEnemy(wanted, bossForm); living++; }
            for (int i = living; i < 1 + Mathf.Min(2, settings.BossEscorts); i++) SpawnEnemy(i % 2 == 0 ? BodyKind.Duelist : BodyKind.Gunner, i);
            MakeBoss(host, bossShiftOrigin);
        }

        private void TickWaves(float delta)
        {
            if (bossShiftQueued && Phase == RunPhase.Fighting) { ResolveBossShift(); if (Phase != RunPhase.Fighting) return; }
            if (Phase == RunPhase.Intermission)
            {
                if(AwaitingExit)return;
                phaseTime += delta;
                if (phaseTime >= settings.WaveBreak) { if (BossPending) StartBoss(); else StartWave(); }
                return;
            }
            int living = 0;
            for (int i = 0; i < bodies.Count; i++) if (bodies[i].Alive && !bodies[i].IsPlayer) living++;
            if (living != 0) return;
            shots.Clear();
            foreach (var body in bodies) body.MarkUntil = 0;
            if(settings.UseRooms && WavesThisRoom>=settings.WavesPerRoom)
            {
                if(Room+1>=settings.RoomCount)
                {
                    if(settings.UseBoss) { BossPending=true; SetPhase(RunPhase.Intermission); }
                    else SetPhase(RunPhase.Won);
                }
                else { AwaitingExit=true; SetPhase(RunPhase.Intermission); }
                return;
            }
            if (Elapsed >= settings.RunDuration) SetPhase(RunPhase.Won);
            else SetPhase(RunPhase.Intermission);
        }
    }
}
