using UnityEngine;

namespace SecondChance.Editor
{
    // Headless checks for knockback/stagger, the possession chain, breakable props, authored waves and the boss.
    public static class FeelChecks
    {
        public static string Run()
        {
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            try
            {
                var game = new GameSession(settings);

                // Hit reactions.
                game.StartRun();
                var duelist = game.Spawn(BodyKind.Duelist, game.Player.Position + new Vector3(1.5f,0,0));
                var brute = game.Spawn(BodyKind.Brute, game.Player.Position + new Vector3(-6,0,0));
                Vector3 before = duelist.Position;
                Require(game.TryAttack(duelist, Vector3.left), "Enemy starts a windup");
                game.Damage(duelist, 5, game.Player, Vector3.right);
                Require(duelist.Stagger > 0 && duelist.Windup <= 0, "Player hit staggers an ordinary enemy and cancels its attack");
                game.Tick(.016f, default); game.Tick(.2f, default);
                Require(duelist.Position.x > before.x + .3f, "Hit knocks the enemy back along the blow");
                Require(game.TryAttack(brute, Vector3.right), "Brute starts a windup");
                game.Damage(brute, 5, game.Player, Vector3.left);
                Require(brute.Stagger <= 0 && brute.Windup > 0, "Brute is not interrupted by hits");

                // Possession chain.
                game.StartRun(); game.Tick(1.1f, default);
                var host = game.Spawn(BodyKind.Brute, game.Player.Position + new Vector3(2,0,0));
                var victim = game.Spawn(BodyKind.Duelist, game.Player.Position + new Vector3(3,0,1));
                game.Damage(host, 1, game.Player); game.Damage(game.Player, 1000);
                Require(game.TryPossess() && game.Player == host, "Chain setup: possession works");
                Require(game.ChainRemaining > 0 && !game.Surging, "Possession opens the chain window");
                game.Damage(victim, 1000, game.Player);
                Require(game.Surging && game.SurgeRemaining > 4, "Kill inside the window starts the surge");
                game.StartRun(); game.Tick(1.1f, default);
                host = game.Spawn(BodyKind.Brute, game.Player.Position + new Vector3(2,0,0));
                victim = game.Spawn(BodyKind.Duelist, game.Player.Position + new Vector3(-12,0,0));
                game.Damage(host, 1, game.Player); game.Damage(game.Player, 1000); game.TryPossess();
                for (int i = 0; i < 150; i++) game.Tick(1f / 60, default);
                Require(game.Phase == RunPhase.Fighting && game.ChainRemaining <= 0, "Chain window closes after its time");
                game.Damage(victim, 1000, game.Player);
                Require(!game.Surging, "Late kill gives no surge");

                // Rooms: authored waves and breakable props.
                settings.UseRooms = true; settings.UseVision = false;
                game = new GameSession(settings);
                game.StartRun();
                Require(game.Bodies.Count == 1 + settings.Waves[0].Count, "First authored wave spawns its exact count");
                Require(game.Props.Count == settings.CratesPerRoom + settings.CanistersPerRoom, "Room places all breakable props, got " + game.Props.Count);
                PropState canister = null, crate = null;
                foreach (var prop in game.Props) { if (prop.Explosive) canister = canister ?? prop; else crate = crate ?? prop; }
                int broken = 0; game.PropHit += (prop, direction) => { if (!prop.Alive) broken++; };
                var side = Side(game, crate.Position, 1.6f, 1.6f);
                var shooter = game.Spawn(BodyKind.Gunner, crate.Position - side * 1.6f, true);
                Require(game.TryAttack(shooter, side), "Test shot starts");
                for (int i = 0; i < 30 && crate.Health >= 30; i++) game.Tick(1f / 60, default);
                Require(crate.Health < 30, "Shots damage a crate instead of passing through");
                side = Side(game, canister.Position, 1.5f, 2.6f);
                var near = game.Spawn(BodyKind.Brute, canister.Position + side * 2.6f);
                var swinger = game.Spawn(BodyKind.Brute, canister.Position - side * 1.5f, true);
                Require((near.Position - swinger.Position).magnitude > 3.6f, "Test bystander stands outside the swing");
                Require(game.TryAttack(swinger, side), "Test swing starts");
                for (int i = 0; i < 30 && canister.Alive; i++) game.Tick(1f / 60, default);
                Require(!canister.Alive && broken >= 1, "Melee breaks a canister");
                Require(near.Health < near.MaxHealth && game.IsMarked(near), "Explosion damages and marks nearby enemies");

                // Boss: three bodies, three different kinds. Enemy damage is negligible so the test player survives.
                settings.UseBoss = true; settings.RoomCount = 1; settings.WavesPerRoom = 1; settings.EnemyDamageMultiplier = .0001f;
                game = new GameSession(settings);
                StartBossFight(game, settings);
                Require(game.BossFight && game.Boss != null && game.Boss.IsBoss && game.BossLivesLeft == 3, "Boss arrives with three lives");
                Require(Mathf.Approximately(game.Boss.MaxHealth, game.Boss.Definition.Health * settings.BossHealthScale), "Boss host has scaled health");
                var kinds = new System.Collections.Generic.HashSet<BodyKind>();
                for (int life = 3; life >= 1; life--)
                {
                    var body = game.Boss;
                    kinds.Add(body.Definition.Kind);
                    game.Tick(1.1f, default);
                    game.Damage(body, 100000, game.Player);
                    Require(!body.Alive, "Boss host can be killed");
                    game.Tick(.02f, default);
                    if (life > 1) Require(game.Phase == RunPhase.Fighting && game.Boss != null && game.Boss != body && game.BossLivesLeft == life - 1, "Boss takes a new host, lives left " + (life - 1));
                }
                Require(kinds.Count == 3, "Boss used three different body kinds");
                Require(game.Phase == RunPhase.Won, "Third boss death wins the run");

                // Possessing the boss's host throws him out and costs him that body.
                StartBossFight(game, settings);
                var bossBody = game.Boss;
                // Isolate possession from random spawn/navigation: put the host on
                // the free entry approach, then expire its initial protection.
                typeof(BodyState).GetProperty("Position").SetValue(bossBody,game.Layout.NearestFree(game.Player.Position+Vector3.forward*2,.7f));
                game.Tick(1.1f,default);
                game.Damage(bossBody,.01f,game.Player);
                Require(game.IsHostReady(bossBody),"Boss fixture is marked, nearby and in clear sight");
                game.Damage(game.Player,100000);
                Require(game.Phase == RunPhase.Possessing && game.Candidate == bossBody, "Marked boss host is a valid candidate");
                Require(game.TryPossess() && game.Player == bossBody && !bossBody.IsBoss, "Player takes the boss host");
                game.Tick(.02f, default);
                Require(game.BossLivesLeft == 2 && game.Boss != null && game.Boss != bossBody, "Evicted boss loses a life and moves on");
                return "Feel checks passed: stagger, knockback, possession chain, authored waves, breakable props, three-body boss";
            }
            finally { Object.DestroyImmediate(settings); }
        }

        private static void StartBossFight(GameSession game, GameSettings settings)
        {
            game.StartRun();
            for (int i = 0; i < game.Bodies.Count; i++)
                if (!game.Bodies[i].IsPlayer) game.Damage(game.Bodies[i], 100000, game.Player);
            game.Tick(.02f, default);
            Require(game.Phase == RunPhase.Intermission && game.BossPending, "Last wave cleared announces the boss");
            game.Tick(settings.WaveBreak + .1f, default);
        }

        // A direction in which both sides of a point are open floor with a clear line between them.
        private static Vector3 Side(GameSession game, Vector3 center, float back, float front)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 a = center - direction * back, b = center + direction * front;
                if (game.Layout.IsFree(a, .7f) && game.Layout.IsFree(b, .7f) && game.Layout.HasSight(a, b)) return direction;
            }
            throw new System.InvalidOperationException("FAILED: no open floor around a test prop");
        }

        private static void Require(bool condition, string description)
        { if (!condition) throw new System.InvalidOperationException("FAILED: " + description); }
    }
}
