using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        private void StartWave()
        {
            Wave++;
            WavesThisRoom++;
            int count = Mathf.Min(settings.MaxWaveSize, settings.FirstWaveSize + (Wave - 1) * settings.WaveGrowth);
            for (int i = 0; i < count; i++)
            {
                var kind = Wave == 1 ? BodyKind.Duelist : (BodyKind)random.Next(3);
                // Each wave after the first exposes all three body options.
                if (Wave > 1 && i < 3) kind = (BodyKind)i;
                var half = settings.ArenaHalfSize;
                float x = (float)(random.NextDouble() * 2 - 1) * (half.x - 1);
                float z = (float)(random.NextDouble() * 2 - 1) * (half.y - 1);
                if (random.Next(2) == 0) x = (i % 2 == 0 ? -1 : 1) * (half.x - 1);
                else z = (i % 2 == 0 ? -1 : 1) * (half.y - 1);
                var position = new Vector3(x, 0, z);
                if(settings.UseRooms && Layout.Spawns.Count>0) position=Layout.Spawns[random.Next(Layout.Spawns.Count)]+new Vector3((float)random.NextDouble()-.5f,0,(float)random.NextDouble()-.5f);
                if (Player != null && (position - Player.Position).sqrMagnitude < 25) position = -position;
                var body = Spawn(kind, position);
                body.AttackCooldown = 1;
            }
            SetPhase(RunPhase.Fighting);
        }

        private void TickWaves(float delta)
        {
            if (Phase == RunPhase.Intermission)
            {
                if(AwaitingExit)return;
                phaseTime += delta;
                if (phaseTime >= settings.WaveBreak) StartWave();
                return;
            }
            int living = 0;
            for (int i = 0; i < bodies.Count; i++) if (bodies[i].Alive && !bodies[i].IsPlayer) living++;
            if (living != 0) return;
            shots.Clear();
            foreach (var body in bodies) body.MarkUntil = 0;
            if(settings.UseRooms && WavesThisRoom>=settings.WavesPerRoom)
            {
                if(Room+1>=settings.RoomCount) SetPhase(RunPhase.Won);
                else { AwaitingExit=true; SetPhase(RunPhase.Intermission); }
                return;
            }
            if (Elapsed >= settings.RunDuration) SetPhase(RunPhase.Won);
            else SetPhase(RunPhase.Intermission);
        }
    }
}
