using System.Collections.Generic;
using UnityEngine;

namespace SecondChance
{
    // All playable cover uses ArenaLayout geometry. Small dressing stays on top of
    // cover or outside the perimeter, so the visible obstacles match AI and sight.
    public sealed partial class RoomDressing : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Material floor, metal, pale, dark, accent, secondary, seam;
        private GameSettings settings;
        private int theme;

        public void Build(ArenaLayout layout,int room,GameSettings configuration)
        {
            settings=configuration; theme=room%3;
            Color color=theme==0 ? new Color(.05f,.85f,.8f) : theme==1 ? new Color(1,.5f,.08f) : new Color(.45f,.9f,.25f);
            Color other=theme==0 ? new Color(.95f,.08f,.45f) : theme==1 ? new Color(.08f,.65f,1) : new Color(.65f,.15f,1);
            floor=Mat(theme==0 ? new Color(.1f,.13f,.16f) : theme==1 ? new Color(.13f,.11f,.1f) : new Color(.075f,.1f,.13f));
            metal=Mat(new Color(.18f,.22f,.28f)); pale=Mat(new Color(.43f,.5f,.53f));
            dark=Mat(new Color(.035f,.045f,.06f)); seam=Mat(new Color(.2f,.23f,.26f));
            accent=Mat(color,1.1f); secondary=Mat(other,1.1f);
            float x=layout.HalfSize.x,z=layout.HalfSize.y;
            Box("Exterior ground",new Vector3(0,-.34f,0),new Vector3(x*2+32,.4f,z*2+32),dark);
            Box("Room floor",new Vector3(0,-.12f,0),new Vector3(x*2,.2f,z*2),floor);
            for(float a=-x+3;a<x;a+=3)Box("Floor joint",new Vector3(a,-.012f,0),new Vector3(.025f,.012f,z*2),seam);
            for(float a=-z+3;a<z;a+=3)Box("Floor joint",new Vector3(0,-.012f,a),new Vector3(x*2,.012f,.025f),seam);
            foreach(float side in new[]{-1f,1f})
            {
                Box("Perimeter",new Vector3(side*(x+.25f),.3f,0),new Vector3(.5f,.6f,z*2+1),metal);
                Box("Perimeter",new Vector3(0,.3f,side*(z+.25f)),new Vector3(x*2,.6f,.5f),metal);
                Box("Edge neon",new Vector3(side*(x+.25f),.61f,0),new Vector3(.08f,.04f,z*2),accent);
                Box("Edge neon",new Vector3(0,.61f,side*(z+.25f)),new Vector3(x*2,.04f,.08f),secondary);
            }
            foreach(var wall in layout.Walls) Obstacle(wall);
            Details(layout,room);
            // Different floor language: clinic centre line, loading lanes, server cables.
            for(float a=-z+2;a<z-2;a+=2)
                Box("Route marking",new Vector3(theme==1 ? -x*.7f : 1.2f,.012f,a),new Vector3(.1f,.012f,1),theme==1 ? secondary : accent);
            if(theme==1)
                for(float a=-x+2;a<x-2;a+=2)
                    Box("Loading lane",new Vector3(a,.012f,-3.8f),new Vector3(1,.012f,.1f),accent);
            if(theme==2)
                foreach(float a in new[]{-x*.6f,0,x*.6f})
                    Box("Cable channel",new Vector3(a+.9f,.02f,0),new Vector3(.12f,.025f,z*2-3),secondary);
            // Low exterior equipment leaves the player readable even near the south edge.
            foreach(float side in new[]{-1f,1f})
            {
                for(float a=-x+3;a<x;a+=7)
                {
                    var pos=new Vector3(a,0,side*(z+3.5f));
                    Box("Exterior service block",pos+Vector3.up*.65f,new Vector3(3,1.3f,1.8f),metal);
                    Model(settings.VentProp,pos+Vector3.up*1.3f,new Vector2(2.4f,1.5f),.8f);
                    Box("Window strip",pos+new Vector3(0,1,side>0 ? -1 : 1),new Vector3(2.4f,.15f,.04f),secondary);
                }
                for(float a=-z+3;a<z;a+=8)
                {
                    var pos=new Vector3(side*(x+3.5f),0,a);
                    Model(settings.PipeProp,pos,new Vector2(2,3),1.5f);
                    Box("Service pillar",pos+Vector3.up*.6f,new Vector3(1.4f,1.2f,1.8f),dark);
                }
            }
            Label(layout.Name,new Vector3(0,.025f,z-3.4f),2.4f,color);
            Label("ВЫХОД",layout.Exit+new Vector3(0,.04f,1),.85f,new Color(.3f,1,1));
            foreach(float side in new[]{-1f,1f})
                Model(settings.StreetLightProp,new Vector3(side*(x+1.4f),0,z-3),new Vector2(1.4f,1.4f),3);
            var fixedObjects=new List<GameObject>();
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                var filter=renderer.GetComponent<MeshFilter>();
                if(renderer.GetComponent<TextMesh>()==null && filter!=null && filter.sharedMesh!=null && filter.sharedMesh.isReadable)
                    fixedObjects.Add(renderer.gameObject);
            }
            if(fixedObjects.Count>0)StaticBatchingUtility.Combine(fixedObjects.ToArray(),gameObject);
        }

        private void Obstacle(ArenaWall wall)
        {
            var p=new Vector3(wall.Center.x,0,wall.Center.y);
            float w=wall.Size.x,d=wall.Size.y;
            if(wall.Kind==ObstacleKind.Wall)
            {
                Box("Solid partition",p+Vector3.up*.72f,new Vector3(w,1.44f,d),theme==0 ? pale : metal);
                Box("Partition trim",p+Vector3.up*1.45f,new Vector3(w+.02f,.035f,d+.02f),accent);
            }
            else if(wall.Kind==ObstacleKind.Table)
            {
                Box("Table pedestal",p+Vector3.up*.42f,new Vector3(w*.5f,.84f,d*.65f),dark);
                Box("Work surface",p+Vector3.up*.9f,new Vector3(w,.18f,d),pale);
                Box("Inset",p+Vector3.up*1.01f,new Vector3(w*.8f,.05f,d*.6f),metal);
                foreach(float side in new[]{-.25f,.25f})
                    Box("Table strap",p+new Vector3(side*w,1.05f,0),new Vector3(.16f,.03f,d*.8f),dark);
                Model(settings.TerminalProp,p+new Vector3(w*.3f,1,0),new Vector2(w*.25f,d*.7f),.6f);
            }
            else if(wall.Kind==ObstacleKind.Supplies)
            {
                Box("Supply pallet",p+Vector3.up*.08f,new Vector3(w,.16f,d),metal);
                Model(theme==0 ? settings.MedicalProp : settings.CrateProp,p+new Vector3(-.4f,.16f,0),new Vector2(.9f,1.1f),.85f);
                Model(theme==2 ? settings.ConsoleProp : settings.TankProp,p+new Vector3(.45f,.16f,0),new Vector2(.7f,1),1.35f);
                Box("Supply status",p+new Vector3(0,.18f,-d*.5f),new Vector3(w*.8f,.05f,.06f),secondary);
            }
            else if(wall.Kind==ObstacleKind.Server)
            {
                Box("Server row",p+Vector3.up*.9f,new Vector3(w,1.8f,d),dark);
                foreach(float side in new[]{-1f,1f})
                    Box("Server top rail",p+new Vector3(side*w*.46f,1.82f,0),new Vector3(.06f,.035f,d*.95f),accent);
                for(float a=-d*.5f+.5f;a<d*.5f;a+=1)
                {
                    Box("Rack panel",p+new Vector3(0,1,a),new Vector3(w+.025f,1.3f,.8f),metal);
                    Box("Rack status",p+new Vector3(0,1.67f,a),new Vector3(w+.04f,.05f,.7f),accent);
                    Box("Rack indicator",p+new Vector3(0,.7f,a),new Vector3(w+.04f,.045f,.7f),secondary);
                    Box("Cooling grille",p+new Vector3(0,1.81f,a),new Vector3(w*.65f,.025f,.55f),metal);
                }
            }
            else
            {
                Box("Equipment plinth",p+Vector3.up*.4f,new Vector3(w,.8f,d),metal);
                foreach(float side in new[]{-1f,1f})
                {
                    Box("Hazard rim",p+new Vector3(side*w*.48f,.82f,0),new Vector3(.065f,.04f,d),accent);
                    Box("Hazard rim",p+new Vector3(0,.82f,side*d*.48f),new Vector3(w,.04f,.065f),accent);
                }
                int count=Mathf.Max(1,Mathf.FloorToInt(w/2));
                for(int i=0;i<count;i++)
                    Model(settings.VentProp,p+new Vector3(-w*.5f+w*(i+.5f)/count,.85f,0),new Vector2(w/count*.9f,d*.9f),1.1f);
            }
        }

        private Material Mat(Color color,float glow=0)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.enableInstancing=true;
            material.color=color;
            if(glow>0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*glow); }
            materials.Add(material);return material;
        }
        private void Box(string name,Vector3 position,Vector3 size,Material material)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;
            box.transform.SetParent(transform,false);box.transform.localPosition=position;box.transform.localScale=size;
            box.GetComponent<Renderer>().sharedMaterial=material;
            if(Application.isPlaying)Destroy(box.GetComponent<Collider>());else DestroyImmediate(box.GetComponent<Collider>());
        }
        private void Model(GameObject prefab,Vector3 position,Vector2 footprint,float height)
        {
            if(prefab==null)return;
            var model=Instantiate(prefab,transform);model.name=prefab.name+" dressing";
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
            var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=Mathf.Min(height/Mathf.Max(.01f,bounds.size.y),footprint.x/Mathf.Max(.01f,bounds.size.x),footprint.y/Mathf.Max(.01f,bounds.size.z));
            model.transform.localScale=Vector3.one*scale;
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            model.transform.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var collider in model.GetComponentsInChildren<Collider>())
                if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
        }
        private void Label(string value,Vector3 position,float height,Color color)
        {
            var label=new GameObject(value).AddComponent<TextMesh>();label.transform.SetParent(transform,false);
            label.transform.localPosition=position;label.transform.localRotation=Quaternion.Euler(90,0,0);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.GetComponent<Renderer>().sharedMaterial=label.font.material;
            label.text=value;label.fontSize=64;label.characterSize=height/64;label.anchor=TextAnchor.MiddleCenter;label.color=color;
        }
        private void OnDestroy() { foreach(var material in materials)if(material!=null) {if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);} }
    }
}
