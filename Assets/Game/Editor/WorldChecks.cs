using System;
using UnityEngine;

namespace SecondChance.Editor
{
    public static class WorldChecks
    {
        public static string Run()
        {
            var map=new ArenaLayout(new Vector2(15,10),0);
            var blocked=map.Move(new Vector3(-11,0,-1),Vector3.forward*4,.45f);
            Require(blocked.z<-.7f,"Movement and dash cannot cross a thin wall");
            var door=map.Move(new Vector3(0,0,-1),Vector3.forward*4,.65f);
            Require(door.z>2.5f,"Doorway admits the largest body");
            Require(!map.HasSight(new Vector3(-11,0,-1),new Vector3(-11,0,1)),"Walls block sight");
            Require(map.HasSight(new Vector3(0,0,-1),new Vector3(0,0,1)),"Doorway preserves sight");
            Require(map.CastFraction(new Vector3(-11,0,-1),new Vector3(-11,0,2),.1f)<.5f,"Projectiles stop at the wall");
            int routes=0;
            for(int room=0;room<3;room++)
            {
                var layout=new ArenaLayout(new Vector2(15,10),room);
                foreach(var spawn in layout.Spawns)
                {
                    var position=spawn;
                    for(int step=0;step<800 && (position-layout.Entry).sqrMagnitude>1;step++)
                        position=layout.Move(position,layout.Route(position,layout.Entry,.65f)*.12f,.65f);
                    Require((position-layout.Entry).sqrMagnitude<=1,"AI route failed in room "+room+" at "+spawn);
                    routes++;
                }
            }
            var settings=ScriptableObject.CreateInstance<GameSettings>();
            try
            {
                settings.UseRooms=true;settings.UseVision=true;settings.FirstWaveSize=0;settings.WaveGrowth=0;settings.WavesPerRoom=1;
                // Formula waves and no boss: this check is about rooms, sight and exits only.
                settings.Waves=new WaveDefinition[0];settings.UseBoss=false;
                var game=new GameSession(settings);game.StartRun();
                Require(game.CanSeePoint(new Vector3(0,0,-4)),"Point in forward cone is visible");
                Require(!game.CanSeePoint(new Vector3(0,0,-9)),"Point behind player is hidden");
                Require(!game.CanSeePoint(new Vector3(-8,0,2)),"In-cone point behind wall is hidden");
                game.Tick(.1f,default);
                Require(game.AwaitingExit,"Cleared room waits for exit interaction");
                float health=game.Player.Health;
                for(int stage=0;stage<2;stage++)
                {
                    for(int step=0;step<1200 && (game.Player.Position-game.Layout.Exit).sqrMagnitude>4;step++)
                        game.Tick(.05f,new PlayerCommand { Move=game.Layout.Route(game.Player.Position,game.Layout.Exit,game.Player.Definition.Radius), Aim=game.Layout.Exit });
                    game.Tick(.01f,new PlayerCommand { Possess=true });
                    Require(game.Room==stage+1,"Exit advances to next room");
                }
                Require(game.Phase==RunPhase.Won && game.Player.Health==health,"Last room wins and transitions preserve body health");

                settings.FirstWaveSize=1;
                game.StartRun();game.Tick(1.1f,default);
                var attacker=game.Spawn(BodyKind.Duelist,new Vector3(-11,0,-.9f),true);
                var victim=game.Spawn(BodyKind.Brute,new Vector3(-11,0,1.1f));
                Require(game.TryAttack(attacker,Vector3.forward),"Test melee attack must actually start");game.Tick(.1f,default);
                Require(victim.Health==victim.Definition.Health,"Melee cannot damage through walls");
                return "World checks passed: collision, sight, projectile barrier, "+routes+" AI routes, room progression, melee occlusion";
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
