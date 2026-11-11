using UnityEngine;

namespace SecondChance
{
    public sealed class RoomView : MonoBehaviour
    {
        private GameSession session;
        private Transform roomRoot;
        private Material wallMaterial,trimMaterial,exitMaterial;
        private LineRenderer exitRing;
        public void Initialize(GameSession game,Material lineMaterial)
        {
            session=game;
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            wallMaterial=new Material(shader) { color=new Color(.14f,.17f,.23f) };
            trimMaterial=new Material(shader) { color=new Color(.1f,.6f,.7f) };
            exitMaterial=new Material(lineMaterial);
            session.RoomChanged+=Build;
            Build(0);
        }
        private void Build(int index)
        {
            if(roomRoot!=null)Destroy(roomRoot.gameObject);
            roomRoot=new GameObject("Room layout "+(index+1)).transform;
            roomRoot.SetParent(transform,false);
            foreach(var wall in session.Layout.Walls)
            {
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name="Solid sight-blocking wall";block.transform.SetParent(roomRoot,false);
                block.transform.position=new Vector3(wall.Center.x,.8f,wall.Center.y);
                block.transform.localScale=new Vector3(wall.Size.x,1.6f,wall.Size.y);
                block.GetComponent<Renderer>().sharedMaterial=wallMaterial;
                var trim=GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name="Wall top strip";trim.transform.SetParent(roomRoot,false);
                trim.transform.position=new Vector3(wall.Center.x,1.61f,wall.Center.y);
                trim.transform.localScale=new Vector3(wall.Size.x+.02f,.035f,wall.Size.y+.02f);
                trim.GetComponent<Renderer>().sharedMaterial=trimMaterial;
                Destroy(trim.GetComponent<Collider>());
            }
            var marker=new GameObject("Room exit");marker.transform.SetParent(roomRoot,false);
            marker.transform.position=session.Layout.Exit;
            exitRing=marker.AddComponent<LineRenderer>();
            exitRing.sharedMaterial=exitMaterial;
            exitRing.useWorldSpace=false;exitRing.widthMultiplier=.13f;exitRing.positionCount=5;
            exitRing.SetPositions(new[] { new Vector3(-.9f,.08f,-.6f),new Vector3(-.9f,.08f,.6f),new Vector3(.9f,.08f,.6f),new Vector3(.9f,.08f,-.6f),new Vector3(-.9f,.08f,-.6f) });
            if(!session.Settings.UseRooms) marker.SetActive(false);
        }
        private void LateUpdate()
        {
            if(exitRing==null)return;
            var color=session.AwaitingExit ? Color.cyan : new Color(.35f,.2f,.2f);
            exitRing.startColor=exitRing.endColor=color;
        }
        private void OnDestroy()
        {
            if(session!=null)session.RoomChanged-=Build;
            if(wallMaterial!=null)Destroy(wallMaterial);
            if(trimMaterial!=null)Destroy(trimMaterial);
            if(exitMaterial!=null)Destroy(exitMaterial);
        }
    }
}
