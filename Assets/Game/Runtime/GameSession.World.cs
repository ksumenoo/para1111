using UnityEngine;

namespace SecondChance
{
    public sealed partial class GameSession
    {
        public ArenaLayout Layout { get; private set; }
        public int Room { get; private set; }
        public int WavesThisRoom { get; private set; }
        public bool AwaitingExit { get; private set; }
        public event System.Action<int> RoomChanged;

        private void ConfigureRoom(int room)
        {
            Room=room; WavesThisRoom=0; AwaitingExit=false;
            Layout=new ArenaLayout(settings.ArenaHalfSize,settings.UseRooms ? room : -1);
            RoomChanged?.Invoke(room);
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
