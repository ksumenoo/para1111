using UnityEngine;

namespace SecondChance
{
    public sealed partial class DefaultHud
    {
        private RectTransform safeRoot, exitPointer;
        private UnityEngine.UI.Text exitLabel;
        private Camera viewCamera;
        private int lastWidth,lastHeight;
        private Rect lastSafeArea;
        private float bossBarTop=.943f,nextTextUpdate,lastHealth=-1;
        private bool lastPaused;
        private RunPhase lastPhase=(RunPhase)(-1);

        private void InitializeResponsive()
        {
            safeRoot=new GameObject("HUD safe area",typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvas.transform,false);safeRoot.anchorMin=Vector2.zero;safeRoot.anchorMax=Vector2.one;
            safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            viewCamera=Camera.main;
        }
        private void RefreshResponsive()
        {
            if(lastWidth==Screen.width && lastHeight==Screen.height && lastSafeArea==Screen.safeArea)return;
            ApplyViewport(Screen.width,Screen.height,Screen.safeArea);
        }
        public void ApplyViewport(int width,int height,Rect area)
        {
            lastWidth=Mathf.Max(1,width);lastHeight=Mathf.Max(1,height);lastSafeArea=area;
            if(area.width<=0 || area.height<=0)area=new Rect(0,0,lastWidth,lastHeight);
            safeRoot.anchorMin=new Vector2(area.xMin/lastWidth,area.yMin/lastHeight);
            safeRoot.anchorMax=new Vector2(area.xMax/lastWidth,area.yMax/lastHeight);
            bool compact=area.width/area.height<1.45f;
            stats.rectTransform.anchorMax=new Vector2(.44f,.98f);
            bossLabel.rectTransform.anchorMin=new Vector2(.49f,compact ? .87f : .945f);
            bossLabel.rectTransform.anchorMax=new Vector2(.975f,compact ? .91f : .99f);
            bossBack.rectTransform.anchorMin=new Vector2(.5f,compact ? .845f : .925f);
            bossBack.rectTransform.anchorMax=new Vector2(.9f,compact ? .86f : .943f);
            bossBar.rectTransform.anchorMin=bossBack.rectTransform.anchorMin;
            bossBar.rectTransform.anchorMax=bossBack.rectTransform.anchorMax;
            bossBarTop=bossBack.rectTransform.anchorMax.y;
            nextTextUpdate=0;
        }
        private bool ShouldRefreshText(bool paused,RunPhase phase,BodyState player)
        {
            float hp=player!=null ? player.Health : -1;
            if(Time.unscaledTime<nextTextUpdate && paused==lastPaused && phase==lastPhase && hp==lastHealth)return false;
            nextTextUpdate=Time.unscaledTime+.08f;lastPaused=paused;lastPhase=phase;lastHealth=hp;return true;
        }
        private void InitializeExitGuide()
        {
            exitPointer=Rect("Exit direction",new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            exitPointer.sizeDelta=new Vector2(60,60);
            var graphic=exitPointer.gameObject.AddComponent<ExitArrowGraphic>();graphic.color=new Color(.2f,1,1);graphic.raycastTarget=false;
            exitLabel=Text("Exit distance",new Vector2(.5f,.5f),new Vector2(.5f,.5f),26,TextAnchor.MiddleCenter);
            exitLabel.rectTransform.sizeDelta=new Vector2(240,42);
            exitLabel.color=new Color(.4f,1,1);
        }
        private void RefreshExitGuide(bool paused)
        {
            bool active=!paused && session.AwaitingExit && session.Player!=null;
            exitPointer.gameObject.SetActive(active);exitLabel.gameObject.SetActive(active);
            if(!active || viewCamera==null)return;
            var point=viewCamera.WorldToScreenPoint(session.Layout.Exit);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot,point,null,out var position);
            var rect=safeRoot.rect;
            var minimum=rect.min+new Vector2(130,140);var maximum=rect.max-new Vector2(130,230);
            // In view: point down at the doorway. Offscreen: stay inside the safe frame.
            bool inside=position.x>=minimum.x && position.x<=maximum.x && position.y>=minimum.y && position.y<=maximum.y;
            var playerScreen=viewCamera.WorldToScreenPoint(session.Player.Position);
            Vector2 direction=new Vector2(point.x-playerScreen.x,point.y-playerScreen.y);
            if(inside) {position+=Vector2.up*65;direction=Vector2.down;}
            position.x=Mathf.Clamp(position.x,minimum.x,maximum.x);position.y=Mathf.Clamp(position.y,minimum.y,maximum.y);
            exitPointer.anchoredPosition=position;
            exitPointer.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
            exitLabel.rectTransform.anchoredPosition=position+Vector2.down*52;
            exitLabel.text="ВЫХОД · "+Mathf.CeilToInt(Vector3.Distance(session.Player.Position,session.Layout.Exit))+" м";
        }
    }
}
