using UnityEngine;

namespace SecondChance
{
    public sealed class RoomView : MonoBehaviour
    {
        private GameSession session;
        private Transform roomRoot;
        private Material exitMaterial;
        private LineRenderer exitRing;
        public void Initialize(GameSession game,Material lineMaterial)
        {
            session=game;
            exitMaterial=new Material(lineMaterial);
            session.RoomChanged+=Build;
            Build(0);
        }
        private void Build(int index)
        {
            if(roomRoot!=null)Destroy(roomRoot.gameObject);
            roomRoot=new GameObject("Room layout "+(index+1)).transform;
            roomRoot.SetParent(transform,false);
            roomRoot.gameObject.AddComponent<RoomDressing>().Build(session.Layout,index,session.Settings);
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
            if(exitRing==null || session==null)return;
            var color=session.AwaitingExit ? Color.cyan : new Color(.35f,.2f,.2f);
            exitRing.startColor=exitRing.endColor=color;
        }
        private void OnDestroy()
        {
            if(session!=null)session.RoomChanged-=Build;
            if(exitMaterial!=null)Destroy(exitMaterial);
        }
    }
}
