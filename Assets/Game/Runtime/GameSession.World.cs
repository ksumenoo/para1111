using System.Collections.Generic;
using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        private readonly List<PropState> props = new List<PropState>(16);
        public IReadOnlyList<PropState> Props => props;
        public ArenaLayout Layout { get; private set; }
        public int Room { get; private set; }
        public int WavesThisRoom { get; private set; }
        public bool AwaitingExit { get; private set; }
        public event System.Action<int> RoomChanged;
        // Raised for every damaged prop; check prop.Alive to tell a dent from destruction.
        public event System.Action<PropState,Vector3> PropHit;

        private void ConfigureRoom(int room)
        {
            Room=room; WavesThisRoom=0; AwaitingExit=false;
            Layout=new ArenaLayout(settings.ArenaHalfSize,settings.UseRooms ? room : -1);
            BuildProps(room);
            RoomChanged?.Invoke(room);
        }

        // Deterministic per room, so a restart shows the same furniture.
        private void BuildProps(int room)
        {
            props.Clear();
            if(!settings.UseRooms)return;
            var dice=new System.Random(room*7919+17);
            int crates=Mathf.Max(0,settings.CratesPerRoom),total=crates+Mathf.Max(0,settings.CanistersPerRoom);
            for(int attempt=0;attempt<600 && props.Count<total;attempt++)
            {
                var point=new Vector3((float)(dice.NextDouble()*2-1)*(Layout.HalfSize.x-2),0,(float)(dice.NextDouble()*2-1)*(Layout.HalfSize.y-2));
                if(!Layout.IsFree(point,1.25f) || (point-Layout.Entry).sqrMagnitude<30 || (point-Layout.Exit).sqrMagnitude<16)continue;
                bool crowded=false;
                foreach(var other in props) if((other.Position-point).sqrMagnitude<12){crowded=true;break;}
                foreach(var spawn in Layout.Spawns) if((spawn-point).sqrMagnitude<6){crowded=true;break;}
                if(crowded)continue;
                bool explosive=props.Count>=crates;
                props.Add(new PropState { Id=props.Count+1, Position=point, Radius=explosive ? .45f : .6f, Health=explosive ? 20 : 30, Explosive=explosive });
            }
        }

        private void CollideProps()
        {
            for(int i=0;i<props.Count;i++)
            {
                var prop=props[i];
                if(!prop.Alive)continue;
                for(int j=0;j<bodies.Count;j++)
                {
                    var body=bodies[j];
                    if(!body.Alive)continue;
                    var difference=body.Position-prop.Position;
                    float minimum=prop.Radius+body.Definition.Radius,distance=difference.magnitude;
                    if(distance>=minimum)continue;
                    // A slight sideways bias makes a head-on walker slide around instead of sticking.
                    var away=distance>.001f ? difference/distance : -body.Facing;
                    MoveBody(body,(away+Vector3.Cross(Vector3.up,away)*.25f)*(minimum-distance));
                }
            }
        }

        private void EnterNextRoom()
        {
            shots.Clear(); bodies.RemoveAll(b=>b!=Player);
            ConfigureRoom(Room+1);
            Player.Position=Layout.Entry; Player.MarkUntil=0; Player.Windup=0;
            Player.Knockback=Vector3.zero; Player.DashTime=0; Player.Invulnerable=1;
            StartWave();
        }

        public bool CanSeePoint(Vector3 position)
        {
            if(!settings.UseVision || Player==null) return true;
            if(Phase==RunPhase.Possessing) return false;
            var direction=position-Player.Position; direction.y=0;
            if(direction.sqrMagnitude>settings.VisionDistance*settings.VisionDistance) return false;
            if(direction.sqrMagnitude>.01f && Vector3.Dot(direction.normalized,Player.Facing)<Mathf.Cos(settings.VisionAngle*.5f*Mathf.Deg2Rad)) return false;
            return Layout.HasSight(Player.Position,position);
        }

        public bool IsVisible(BodyState body)
        {
            if(!settings.UseVision)return true;
            if(body.IsPlayer) return true;
            if(Phase==RunPhase.Possessing) return CanPossess(body);
            return CanSeePoint(body.Position);
        }

        private void MoveBody(BodyState body,Vector3 displacement)
        { body.Position=Layout.Move(body.Position,displacement,body.Definition.Radius); }
    }
}
