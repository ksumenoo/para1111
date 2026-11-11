using UnityEngine;

namespace SecondChance
{
    public sealed partial class RoomDressing
    {
        private void Details(ArenaLayout layout,int room)
        {
            foreach(var wall in layout.Walls)
            {
                var p=new Vector3(wall.Center.x,0,wall.Center.y);
                if(wall.Kind==ObstacleKind.Table)
                {
                    Model(settings.MedicalProp,p+new Vector3(-wall.Size.x*.28f,1.06f,0),new Vector2(.55f,.5f),.32f);
                    for(int i=0;i<3;i++)
                        Box("Instruments",p+new Vector3(-.2f+i*.13f,1.08f,-wall.Size.y*.22f),new Vector3(.045f,.025f,.27f),pale);
                    Box("Diagnostic screen",p+new Vector3(.1f,1.1f,wall.Size.y*.25f),new Vector3(.5f,.035f,.3f),accent);
                }
                else if(wall.Kind==ObstacleKind.Wall)
                {
                    bool horizontal=wall.Size.x>wall.Size.y;
                    int panels=Mathf.Max(1,Mathf.FloorToInt(Mathf.Max(wall.Size.x,wall.Size.y)/3));
                    for(int i=0;i<panels;i++)
                    {
                        float along=(i+.5f)/panels-.5f;
                        var point=p+new Vector3(horizontal ? along*wall.Size.x : 0,1.48f,horizontal ? 0 : along*wall.Size.y);
                        Model(i%2==0 ? settings.ScreenProp : settings.PanelLightProp,point,new Vector2(horizontal ? 1.15f : .38f,horizontal ? .38f : 1.15f),.35f);
                    }
                    if(horizontal && wall.Size.x>5)
                    {
                        var sign=new GameObject("Clinic wall sign").AddComponent<TextMesh>();sign.transform.SetParent(transform,false);
                        sign.transform.position=p+new Vector3(0,.9f,-wall.Size.y*.5f-.015f);
                        sign.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");sign.GetComponent<Renderer>().sharedMaterial=sign.font.material;
                        sign.text=wall.Center.x<0 ? "+  ОТТИСК  /  МЕДБЛОК" : "НЕЙРОХИРУРГИЯ  +";
                        sign.fontSize=64;sign.characterSize=.065f;sign.anchor=TextAnchor.MiddleCenter;sign.color=new Color(.2f,1,.9f);
                    }
                }
                else if(wall.Kind==ObstacleKind.Machinery)
                {
                    Model(settings.TankProp,p+new Vector3(-wall.Size.x*.25f,.86f,0),new Vector2(.55f,wall.Size.y*.6f),1.25f);
                    Model(settings.CableProp,p+new Vector3(0,.86f,wall.Size.y*.33f),new Vector2(wall.Size.x*.8f,.22f),.15f);
                }
                else if(wall.Kind==ObstacleKind.Server)
                {
                    Model(settings.ConsoleProp,p+new Vector3(0,1.84f,0),new Vector2(wall.Size.x*.8f,1.2f),.65f);
                    Model(settings.CableProp,p+new Vector3(0,1.83f,-wall.Size.y*.3f),new Vector2(wall.Size.x*.8f,1.4f),.18f);
                }
            }
            // Flat litter gives the empty floor texture without invisible obstacles.
            var dice=new System.Random(room*131+7);
            var stain=Mat(new Color(.055f,.06f,.07f));
            for(int i=0;i<110;i++)
            {
                var p=new Vector3((float)(dice.NextDouble()*2-1)*(layout.HalfSize.x-1),0,(float)(dice.NextDouble()*2-1)*(layout.HalfSize.y-1));
                if(!layout.IsFree(p,.75f) || (p-layout.Entry).sqrMagnitude<4 || (p-layout.Exit).sqrMagnitude<5)continue;
                float roll=(float)dice.NextDouble();
                if(i%5==0)Box("Floor wear",p+Vector3.up*.006f,new Vector3(.5f+roll,.008f,.3f+roll*.5f),stain);
                else if(i%5==1)Model(settings.CableProp,p+Vector3.up*.02f,new Vector2(1.2f,.6f),.035f);
                else if(i%5==2)Box("Metal offcut",p+Vector3.up*.015f,new Vector3(.12f,.025f,.45f+roll*.3f),metal);
                else if(i%5==3)Box(theme==0 ? "Medical wrapper" : "Discarded label",p+Vector3.up*.009f,new Vector3(.22f,.012f,.3f),pale);
                else Box("Floor repair plate",p+Vector3.up*.008f,new Vector3(.45f,.012f,.65f),seam);
            }
            // Service panels on perimeter: visible from the playable floor, not only outside.
            for(float a=-layout.HalfSize.x+3;a<layout.HalfSize.x-2;a+=4)
            foreach(float side in new[]{-1f,1f})
            {
                var point=new Vector3(a,.62f,side*(layout.HalfSize.y+.25f));
                Model(settings.PanelLightProp,point,new Vector2(1.3f,.42f),.22f);
            }
        }
    }
}
