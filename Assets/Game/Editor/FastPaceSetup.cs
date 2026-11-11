using System;
using UnityEditor;
using UnityEngine;

namespace SecondChance.Editor
{
    public static class FastPaceSetup
    {
        public static void Apply()
        {
            const string path="Assets/Game/Settings/GameSettings.asset";
            var s=AssetDatabase.LoadAssetAtPath<GameSettings>(path);
            if(!System.IO.File.Exists("Logs/GameSettings-before-fast.asset"))System.IO.File.Copy(path,"Logs/GameSettings-before-fast.asset");
            s.DashDuration=.14f;s.DashCooldown=.62f;s.DashSpeed=24;
            s.WaveBreak=1.3f;s.ProjectileSpeed=24;s.HitStop=.018f;s.HeavyHitStop=.045f;s.HitStagger=.14f;
            s.EnemyDamageMultiplier=.65f;s.CameraDamping=.12f;s.BossHealthScale=1.8f;
            var d=s.GetBody(BodyKind.Duelist);d.Health=55;d.Speed=8.5f;d.Damage=34;d.AttackCooldown=.23f;d.PlayerWindup=.035f;d.EnemyWindup=.28f;
            var b=s.GetBody(BodyKind.Brute);b.Health=100;b.Speed=5.6f;b.Damage=68;b.AttackCooldown=.55f;b.PlayerWindup=.09f;b.EnemyWindup=.52f;
            var g=s.GetBody(BodyKind.Gunner);g.Health=45;g.Speed=7.5f;g.Damage=30;g.AttackCooldown=.28f;g.PlayerWindup=.035f;g.EnemyWindup=.45f;
            EditorUtility.SetDirty(s);AssetDatabase.SaveAssets();
        }

        public static void Verify()
        {
            var s=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Game/Settings/GameSettings.asset"));
            try
            {
                s.UseRooms=false;s.UseVision=false;s.UseBoss=false;s.FirstWaveSize=0;s.WaveGrowth=0;s.Waves=Array.Empty<WaveDefinition>();
                var game=new GameSession(s);game.StartRun();
                game.Tick(.1f,new PlayerCommand {Move=Vector3.right,Aim=Vector3.right*10});
                if(Mathf.Abs(game.Player.Position.x-.85f)>.01f)throw new Exception("Unexpected fast movement speed");
                game.StartRun();var enemy=game.Spawn(BodyKind.Duelist,new Vector3(0,0,1.5f));
                if(!game.TryAttack(game.Player,Vector3.forward))throw new Exception("Attack did not start");
                for(int i=0;i<5;i++)game.Tick(.01f,new PlayerCommand{Aim=enemy.Position});
                if(!enemy.Alive || !game.IsMarked(enemy))throw new Exception("First strike must leave a marked host");
                for(int i=0;i<26;i++)game.Tick(.01f,new PlayerCommand{Aim=enemy.Position});
                if(!game.TryAttack(game.Player,enemy.Position-game.Player.Position))throw new Exception("Attack cooldown too long");
                for(int i=0;i<5;i++)game.Tick(.01f,new PlayerCommand{Aim=enemy.Position});
                if(enemy.Alive)throw new Exception("Second strike must kill regular enemy");
                if(s.PossessionSeconds!=2)throw new Exception("Death window changed");
                Debug.Log("Fast balance verified: speed 8.5, two-hit kill, first hit leaves marked host, death window 2 seconds");
            }
            finally{UnityEngine.Object.DestroyImmediate(s);}
        }
    }
}
