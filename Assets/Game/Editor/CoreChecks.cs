using UnityEngine;

namespace SecondChance.Editor
{
    public static class CoreChecks
    {
        public static string Run()
        {
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            try
            {
                var game = new GameSession(settings);
                game.StartRun(); game.Tick(1.1f, default);
                var wounded = game.Spawn(BodyKind.Brute, new Vector3(2,0,0));
                game.Damage(wounded, 20, game.Player);
                Require(game.IsMarked(wounded), "Player damage marks a living enemy");
                float hp = wounded.Health;
                var old = game.Player;
                game.Damage(old, 1000);
                Require(game.Phase == RunPhase.Possessing && game.Candidate == wounded, "Lethal damage opens possession with nearest marked candidate");
                float before = game.PossessionRemaining;
                var hostPosition = wounded.Position;
                game.Tick(.5f, default);
                Require(Mathf.Abs(before - game.PossessionRemaining - .5f) < .001f, "Possession timer uses real time");
                float hostMovement = (wounded.Position - hostPosition).magnitude;
                Require(hostMovement > .001f && hostMovement < .3f, "Enemies move slowly during possession instead of freezing");
                Require(game.TryPossess() && game.Player == wounded && wounded.Health == hp && wounded.IsPlayer && !old.Alive, "Possession preserves health, changes body, and kills old body");
                game.Damage(wounded, 1000);
                Require(wounded.Alive, "Possession grants brief protection");

                game.StartRun(); game.Tick(1.1f, default); game.Damage(game.Player, 1000); game.Tick(2.01f, default);
                Require(game.Phase == RunPhase.Lost && !game.TryPossess(), "No host / expired timer loses the run");

                game.StartRun(); game.Tick(1.1f, default);
                var far = game.Spawn(BodyKind.Gunner, new Vector3(14,0,9));
                game.Damage(far, 1, game.Player); game.Damage(game.Player, 1000);
                Require(game.Candidate == null && !game.TryPossess(), "Marked host outside radius is rejected");

                settings.MarkDuration = .1f;
                game.StartRun(); game.Tick(1.1f, default);
                var expired = game.Spawn(BodyKind.Duelist, new Vector3(2,0,0));
                game.Damage(expired, 1, game.Player); game.Tick(.2f, default);
                Require(!game.IsMarked(expired), "Expired marks cannot supply a life");

                game.StartRun(); game.Tick(1.1f, default);
                var ahead = game.Spawn(BodyKind.Duelist, new Vector3(1.7f,0,0));
                var behind = game.Spawn(BodyKind.Duelist, new Vector3(-1.7f,0,0));
                game.TryAttack(game.Player,Vector3.right); game.Tick(.1f,default);
                Require(ahead.Health < ahead.Definition.Health && behind.Health == behind.Definition.Health, "Melee hits forward sector only");
                game.Tick(.1f,new PlayerCommand { Dash = true, Move = Vector3.right });
                float dashHealth = game.Player.Health;
                game.Damage(game.Player,1000);
                Require(game.Player.Health == dashHealth, "Dash protects against lethal damage");

                game.StartRun(); game.Tick(1.1f,default);
                var gunner = game.Spawn(BodyKind.Gunner,new Vector3(2,0,0));
                game.Damage(gunner,1,game.Player); game.Damage(game.Player,1000); game.TryPossess();
                var bulletTarget = game.Spawn(BodyKind.Brute,new Vector3(4,0,0));
                game.TryAttack(game.Player,Vector3.right); game.Tick(.1f,default);
                Require(bulletTarget.Health < bulletTarget.Definition.Health && game.IsMarked(bulletTarget), "Possessed gunner fires a projectile that marks enemies");

                settings.RunDuration = .1f;
                game.StartRun(); game.Tick(1.1f, default);
                for (int i = 0; i < game.Bodies.Count; i++) if (!game.Bodies[i].IsPlayer) game.Damage(game.Bodies[i],1000,game.Player);
                game.Tick(.1f, default);
                Require(game.Phase == RunPhase.Won, "Timer plus cleared arena wins");
                game.StartRun(); Require(game.Phase == RunPhase.Fighting && game.Transfers == 0 && game.Kills == 0 && game.Player.Alive, "Restart resets run state");
                return "14 core behavior checks passed";
            }
            finally { Object.DestroyImmediate(settings); }
        }
        private static void Require(bool condition, string description)
        { if (!condition) throw new System.InvalidOperationException("FAILED: " + description); }
    }
}
