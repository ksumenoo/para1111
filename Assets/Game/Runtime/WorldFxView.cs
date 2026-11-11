using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SecondChance
{
    // Breakable props and floor marks (blood, scorch). Presentation only: PropState lives in GameSession.
    public sealed class WorldFxView : MonoBehaviour
    {
        private const int MaxDecals = 90;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color Blood = new Color(.42f,.02f,.05f,.85f);
        private GameSession session;
        private CombatFeedback feedback;
        private Transform propsRoot, decalsRoot;
        private Material splatMaterial, crate, crateDark, canister, canisterGlow, crateGlow;
        private Texture2D splatTexture;
        private MaterialPropertyBlock block;
        private readonly Dictionary<int,Transform> propViews = new Dictionary<int,Transform>();
        private readonly List<Renderer> decals = new List<Renderer>();
        private int nextDecal;

        public void Initialize(GameSession game, CombatFeedback combatFeedback, Material lineMaterial)
        {
            session = game; feedback = combatFeedback;
            block = new MaterialPropertyBlock();
            splatTexture = MakeSplat();
            splatMaterial = new Material(lineMaterial) { mainTexture = splatTexture };
            crate = Lit(new Color(.3f,.27f,.22f)); crateDark = Lit(new Color(.09f,.09f,.1f));
            crateGlow = Lit(new Color(.1f,.8f,.85f),1.2f);
            canister = Lit(new Color(.7f,.13f,.1f)); canisterGlow = Lit(new Color(1,.55f,.1f),2);
            decalsRoot = new GameObject("Floor marks").transform; decalsRoot.SetParent(transform,false);
            session.RoomChanged += Rebuild;
            session.PropHit += OnPropHit;
            session.Combat += OnCombat;
            Rebuild(session.Room);
        }

        private void Rebuild(int room)
        {
            if (propsRoot != null) Destroy(propsRoot.gameObject);
            propViews.Clear();
            foreach (var decal in decals) if (decal != null) decal.gameObject.SetActive(false);
            propsRoot = new GameObject("Breakable props").transform; propsRoot.SetParent(transform,false);
            var dice = new System.Random(room * 31 + 5);
            foreach (var prop in session.Props)
            {
                var view = new GameObject(prop.Explosive ? "Canister" : "Crate").transform;
                view.SetParent(propsRoot,false); view.position = prop.Position;
                view.rotation = Quaternion.Euler(0,(float)dice.NextDouble() * 360,0);
                if (prop.Explosive)
                {
                    Part(view,PrimitiveType.Cylinder,new Vector3(0,.55f,0),new Vector3(.72f,.55f,.72f),canister);
                    Part(view,PrimitiveType.Cylinder,new Vector3(0,.8f,0),new Vector3(.76f,.05f,.76f),canisterGlow);
                    Part(view,PrimitiveType.Cylinder,new Vector3(0,1.13f,0),new Vector3(.4f,.04f,.4f),crateDark);
                }
                else
                {
                    Part(view,PrimitiveType.Cube,new Vector3(0,.45f,0),new Vector3(.95f,.9f,.95f),crate);
                    Part(view,PrimitiveType.Cube,new Vector3(0,.2f,0),new Vector3(1,.09f,1),crateDark);
                    Part(view,PrimitiveType.Cube,new Vector3(0,.72f,0),new Vector3(1,.09f,1),crateDark);
                    Part(view,PrimitiveType.Cube,new Vector3(0,.91f,0),new Vector3(.5f,.02f,.12f),crateGlow);
                }
                propViews.Add(prop.Id,view);
            }
        }

        private void OnPropHit(PropState prop, Vector3 direction)
        {
            if (!propViews.TryGetValue(prop.Id,out var view) || view == null) return;
            if (feedback != null) feedback.PropImpact(prop);
            if (prop.Alive)
            {
                view.DOKill(); view.localScale = Vector3.one;
                view.DOPunchScale(new Vector3(.18f,-.14f,.18f),.16f,4,.4f).SetUpdate(true);
                return;
            }
            propViews.Remove(prop.Id);
            view.DOKill(); Destroy(view.gameObject);
            // Pieces fly mostly along the blow.
            var push = new Vector3(direction.x,0,direction.z).normalized;
            for (int i = 0; i < (prop.Explosive ? 10 : 7); i++)
            {
                var piece = Part(propsRoot,PrimitiveType.Cube,Vector3.zero,Vector3.one * Random.Range(.16f,.34f),prop.Explosive ? (i % 3 == 0 ? canisterGlow : canister) : (i % 3 == 0 ? crateDark : crate)).transform;
                piece.position = prop.Position + new Vector3(Random.Range(-.3f,.3f),Random.Range(.25f,.9f),Random.Range(-.3f,.3f));
                piece.rotation = Random.rotation;
                var spread = Random.insideUnitSphere; spread.y = 0;
                var target = prop.Position + (push * Random.Range(.4f,1.6f) + spread * (prop.Explosive ? 2.6f : 1.3f));
                target.y = .08f;
                piece.DOJump(target,Random.Range(.5f,1.3f),1,Random.Range(.35f,.55f)).SetEase(Ease.Linear).SetLink(piece.gameObject);
                piece.DOScale(Vector3.zero,.3f).SetDelay(Random.Range(.9f,1.5f)).SetLink(piece.gameObject).OnComplete(() => { if (piece != null) Destroy(piece.gameObject); });
            }
            if (prop.Explosive) Splat(prop.Position,session.Settings.ExplosionRadius * 1.5f,new Color(.02f,.02f,.03f,.8f));
            else Splat(prop.Position,1.3f,new Color(.12f,.1f,.08f,.6f));
        }

        private void OnCombat(CombatEvent message)
        {
            if (message.Cue != CombatCue.Hit && message.Cue != CombatCue.Death) return;
            if (!message.Body.IsPlayer && !session.CanSeePoint(message.Position)) return;
            var along = new Vector3(message.Direction.x,0,message.Direction.z).normalized;
            if (message.Cue == CombatCue.Hit)
                Splat(message.Position + along * Random.Range(.4f,1.1f) + Jitter(.25f),Random.Range(.45f,.85f),Blood);
            else
            {
                Splat(message.Position + Jitter(.15f),Random.Range(1.5f,2),Blood);
                for (int i = 0; i < 3; i++) Splat(message.Position + along * Random.Range(.6f,1.8f) + Jitter(.6f),Random.Range(.5f,1),Blood);
            }
        }

        private static Vector3 Jitter(float amount) => new Vector3(Random.Range(-amount,amount),0,Random.Range(-amount,amount));

        private void Splat(Vector3 position, float size, Color color)
        {
            if (!session.Layout.IsFree(new Vector3(position.x,0,position.z),.05f)) return;
            Renderer decal;
            if (decals.Count < MaxDecals)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Floor mark"; Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(decalsRoot,false);
                decal = quad.GetComponent<Renderer>();
                decal.sharedMaterial = splatMaterial;
                decal.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; decal.receiveShadows = false;
                decals.Add(decal);
            }
            else decal = decals[nextDecal % MaxDecals];
            // Later marks sit a hair higher, so overlapping ones never flicker.
            decal.transform.position = new Vector3(position.x,.022f + (nextDecal % MaxDecals) * .0004f,position.z);
            decal.transform.rotation = Quaternion.Euler(90,Random.Range(0f,360f),0);
            decal.transform.localScale = new Vector3(size,size * Random.Range(.7f,1.1f),1);
            decal.GetPropertyBlock(block); block.SetColor(ColorId,color); decal.SetPropertyBlock(block);
            decal.gameObject.SetActive(true);
            nextDecal++;
        }

        private GameObject Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent,false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static Material Lit(Color color, float glow = 0)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };
            if (glow > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color * glow); }
            return material;
        }

        // Irregular soft blob: one core plus a few satellites.
        private static Texture2D MakeSplat()
        {
            const int size = 64;
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false) { wrapMode = TextureWrapMode.Clamp, name = "Generated splat" };
            var dice = new System.Random(5);
            var centers = new Vector3[9];
            centers[0] = new Vector3(.5f,.5f,.27f);
            for (int i = 1; i < centers.Length; i++)
            {
                float angle = (float)dice.NextDouble() * Mathf.PI * 2, distance = .16f + (float)dice.NextDouble() * .2f;
                centers[i] = new Vector3(.5f + Mathf.Cos(angle) * distance,.5f + Mathf.Sin(angle) * distance,.05f + (float)dice.NextDouble() * .09f);
            }
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size, v = (y + .5f) / size, alpha = 0;
                foreach (var center in centers)
                {
                    float distance = Mathf.Sqrt((u - center.x) * (u - center.x) + (v - center.y) * (v - center.y));
                    alpha = Mathf.Max(alpha,Mathf.Clamp01((center.z - distance) / (center.z * .35f)));
                }
                pixels[y * size + x] = new Color(1,1,1,alpha);
            }
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (session != null) { session.RoomChanged -= Rebuild; session.PropHit -= OnPropHit; session.Combat -= OnCombat; }
            foreach (var material in new[] { splatMaterial, crate, crateDark, canister, canisterGlow, crateGlow }) if (material != null) Destroy(material);
            if (splatTexture != null) Destroy(splatTexture);
        }
    }
}
