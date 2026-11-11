using System.Collections.Generic;
using UnityEngine;

namespace SecondChance
{
    public readonly struct ArenaWall
    {
        public readonly Vector2 Center, Size;
        public ArenaWall(float x,float z,float width,float depth) { Center=new Vector2(x,z); Size=new Vector2(width,depth); }
    }

    // One geometry source for visuals, collision, sight, projectiles and AI routing.
    public sealed class ArenaLayout
    {
        private readonly List<ArenaWall> walls = new List<ArenaWall>();
        public IReadOnlyList<ArenaWall> Walls => walls;
        public Vector2 HalfSize { get; }
        public Vector3 Entry { get; private set; }
        public Vector3 Exit { get; private set; }
        private readonly List<Vector3> spawns = new List<Vector3>();
        public IReadOnlyList<Vector3> Spawns => spawns;
        private readonly int width, depth;
        private readonly int[] distance, queue;
        private readonly bool[] walkable;
        private int cachedGoal = -1;
        private static readonly Vector2Int[] Neighbors = { new Vector2Int(1,0),new Vector2Int(-1,0),new Vector2Int(0,1),new Vector2Int(0,-1),new Vector2Int(1,1),new Vector2Int(-1,1),new Vector2Int(1,-1),new Vector2Int(-1,-1) };

        public ArenaLayout(Vector2 halfSize,int room = -1)
        {
            HalfSize=halfSize; Entry=Vector3.zero; Exit=new Vector3(0,0,halfSize.y-1);
            if (room >= 0) CreateRoom(room % 3);
            width=Mathf.CeilToInt(halfSize.x*2); depth=Mathf.CeilToInt(halfSize.y*2);
            distance=new int[width*depth]; queue=new int[distance.Length]; walkable=new bool[distance.Length];
            for(int i=0;i<walkable.Length;i++) walkable[i]=IsFree(CellPosition(i),.72f);
        }

        private void CreateRoom(int room)
        {
            // Authored at the current 30x20 arena size. Doorways are four units wide.
            if(room==0)
            {
                Entry=new Vector3(0,0,-8); Exit=new Vector3(0,0,8.5f);
                walls.Add(new ArenaWall(-8.5f,0,13,.65f)); walls.Add(new ArenaWall(8.5f,0,13,.65f));
                walls.Add(new ArenaWall(-7,-5,3,2)); walls.Add(new ArenaWall(7,5,3,2));
            }
            else if(room==1)
            {
                Entry=new Vector3(-11,0,-7); Exit=new Vector3(11,0,7);
                walls.Add(new ArenaWall(0,-6.5f,.65f,7)); walls.Add(new ArenaWall(0,5.5f,.65f,9));
                walls.Add(new ArenaWall(-8,3,4,2)); walls.Add(new ArenaWall(8,-4,4,2));
            }
            else
            {
                Entry=new Vector3(0,0,-8); Exit=new Vector3(10,0,8);
                walls.Add(new ArenaWall(-11.5f,-2,7,.65f)); walls.Add(new ArenaWall(0,-2,8,.65f)); walls.Add(new ArenaWall(11.5f,-2,7,.65f));
                walls.Add(new ArenaWall(0,5,.65f,6));
            }
            foreach(var point in new[] { new Vector3(-11,0,7),new Vector3(-5,0,7),new Vector3(5,0,7),new Vector3(11,0,7),new Vector3(-11,0,-7),new Vector3(11,0,-7),new Vector3(-11,0,3),new Vector3(11,0,3) })
                spawns.Add(NearestFree(point,.7f));
        }

        public bool IsFree(Vector3 point,float radius)
        {
            if(Mathf.Abs(point.x)>HalfSize.x-radius || Mathf.Abs(point.z)>HalfSize.y-radius) return false;
            foreach(var wall in walls)
                if(Mathf.Abs(point.x-wall.Center.x)<wall.Size.x*.5f+radius && Mathf.Abs(point.z-wall.Center.y)<wall.Size.y*.5f+radius) return false;
            return true;
        }

        public Vector3 NearestFree(Vector3 point,float radius)
        {
            point=new Vector3(Mathf.Clamp(point.x,-HalfSize.x+radius,HalfSize.x-radius),0,Mathf.Clamp(point.z,-HalfSize.y+radius,HalfSize.y-radius));
            if(IsFree(point,radius)) return point;
            for(float ring=.25f;ring<HalfSize.magnitude*2;ring+=.25f)
            for(int i=0;i<24;i++)
            {
                float angle=i*Mathf.PI*2/24;
                var candidate=point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring;
                if(IsFree(candidate,radius)) return candidate;
            }
            return Entry;
        }

        public Vector3 Move(Vector3 position,Vector3 displacement,float radius)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(displacement.magnitude/Mathf.Max(.05f,radius*.4f)));
            var step=displacement/steps;
            for(int i=0;i<steps;i++)
            {
                var next=position+new Vector3(step.x,0,0); if(IsFree(next,radius)) position=next;
                next=position+new Vector3(0,0,step.z); if(IsFree(next,radius)) position=next;
            }
            return position;
        }

        public bool HasSight(Vector3 from,Vector3 to) => CastFraction(from,to)<.99999f ? false : true;
        public float CastFraction(Vector3 from,Vector3 to,float padding=0)
        {
            float nearest=1;
            var ray=to-from;
            if(to.x>HalfSize.x-padding && ray.x>0)nearest=Mathf.Min(nearest,(HalfSize.x-padding-from.x)/ray.x);
            if(to.x<-HalfSize.x+padding && ray.x<0)nearest=Mathf.Min(nearest,(-HalfSize.x+padding-from.x)/ray.x);
            if(to.z>HalfSize.y-padding && ray.z>0)nearest=Mathf.Min(nearest,(HalfSize.y-padding-from.z)/ray.z);
            if(to.z<-HalfSize.y+padding && ray.z<0)nearest=Mathf.Min(nearest,(-HalfSize.y+padding-from.z)/ray.z);
            foreach(var wall in walls)
            {
                float enter=0,exit=1;
                var half=wall.Size*.5f+Vector2.one*padding;
                if(Slab(from.x,to.x-from.x,wall.Center.x-half.x,wall.Center.x+half.x,ref enter,ref exit)
                    && Slab(from.z,to.z-from.z,wall.Center.y-half.y,wall.Center.y+half.y,ref enter,ref exit)) nearest=Mathf.Min(nearest,enter);
            }
            return nearest;
        }
        private static bool Slab(float start,float delta,float minimum,float maximum,ref float enter,ref float exit)
        {
            if(Mathf.Abs(delta)<.000001f) return start>=minimum && start<=maximum;
            float a=(minimum-start)/delta,b=(maximum-start)/delta;
            if(a>b) { float temp=a;a=b;b=temp; }
            enter=Mathf.Max(enter,a); exit=Mathf.Min(exit,b);
            return enter<=exit && exit>=0 && enter<=1;
        }

        public Vector3 Route(Vector3 from,Vector3 goal,float radius)
        {
            var difference=goal-from;
            if(CastFraction(from,goal,radius)>=.99999f) return difference.sqrMagnitude>.0001f ? difference.normalized : Vector3.zero;
            int target=NearestCell(goal);
            if(target<0) return Vector3.zero;
            if(target!=cachedGoal) BuildField(target);
            int source=NearestCell(from), best=source;
            if(source<0 || distance[source]<0) return Vector3.zero;
            int sx=source%width,sz=source/width;
            foreach(var offset in Neighbors)
            {
                int x=sx+offset.x,z=sz+offset.y;
                if(x<0 || z<0 || x>=width || z>=depth) continue;
                int next=z*width+x;
                if(distance[next]<0 || distance[next]>=distance[best]) continue;
                if(CastFraction(from,CellPosition(next),radius)<.99999f) continue;
                best=next;
            }
            var waypoint=CellPosition(best)-from;
            return waypoint.sqrMagnitude>.01f ? waypoint.normalized : Vector3.zero;
        }
        private Vector3 CellPosition(int index) => new Vector3(-HalfSize.x+.5f+index%width,0,-HalfSize.y+.5f+index/width);
        private int NearestCell(Vector3 point)
        {
            int x=Mathf.Clamp(Mathf.FloorToInt(point.x+HalfSize.x),0,width-1),z=Mathf.Clamp(Mathf.FloorToInt(point.z+HalfSize.y),0,depth-1);
            int direct=z*width+x;
            if(walkable[direct])return direct;
            int best=-1; float nearest=float.MaxValue;
            for(int i=0;i<walkable.Length;i++) if(walkable[i])
            { float squared=(CellPosition(i)-point).sqrMagnitude; if(squared<nearest){nearest=squared;best=i;} }
            return best;
        }
        private void BuildField(int target)
        {
            cachedGoal=target;
            for(int i=0;i<distance.Length;i++) distance[i]=-1;
            int head=0,tail=0;queue[tail++]=target;distance[target]=0;
            while(head<tail)
            {
                int source=queue[head++],sx=source%width,sz=source/width;
                foreach(var offset in Neighbors)
                {
                    int x=sx+offset.x,z=sz+offset.y;
                    if(x<0 || z<0 || x>=width || z>=depth)continue;
                    int next=z*width+x;
                    if(!walkable[next] || distance[next]>=0)continue;
                    if(offset.x!=0 && offset.y!=0 && (!walkable[sz*width+x] || !walkable[z*width+sx]))continue;
                    distance[next]=distance[source]+1; queue[tail++]=next;
                }
            }
        }
    }
}
