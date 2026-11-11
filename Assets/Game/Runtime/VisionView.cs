using UnityEngine;

namespace SecondChance
{
    public sealed class VisionView : MonoBehaviour
    {
        private GameSession session;
        private Mesh mesh;
        private MeshRenderer renderer;
        private Material material;
        private readonly Vector3[] vertices=new Vector3[83];
        public void Initialize(GameSession game,Material lineMaterial)
        {
            session=game;
            var fan=new GameObject("Player field of vision");fan.transform.SetParent(transform,false);
            var filter=fan.AddComponent<MeshFilter>();renderer=fan.AddComponent<MeshRenderer>();
            material=new Material(lineMaterial);renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            mesh=new Mesh { name="Occluded sight cone" };mesh.MarkDynamic();filter.sharedMesh=mesh;
            var triangles=new int[81*3];
            for(int i=0;i<81;i++){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
            mesh.vertices=vertices;mesh.triangles=triangles;
        }
        private void LateUpdate()
        {
            if(session==null)return;
            bool active=session.Settings.UseVision && session.Player!=null && session.Player.Alive && (session.Phase==RunPhase.Fighting || session.Phase==RunPhase.Intermission);
            renderer.enabled=active;
            if(!active)return;
            var origin=session.Player.Position+Vector3.up*.035f;
            vertices[0]=origin;
            for(int i=0;i<82;i++)
            {
                float angle=Mathf.Lerp(-session.Settings.VisionAngle*.5f,session.Settings.VisionAngle*.5f,i/81f);
                var direction=Quaternion.AngleAxis(angle,Vector3.up)*session.Player.Facing;
                var end=origin+direction*session.Settings.VisionDistance;
                vertices[i+1]=Vector3.Lerp(origin,end,session.Layout.CastFraction(origin,end));
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();
            material.color=new Color(.15f,.8f,.85f,.065f);
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
