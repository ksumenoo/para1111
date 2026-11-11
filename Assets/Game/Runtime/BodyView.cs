using DG.Tweening;
using UnityEngine;

namespace SecondChance
{
    public sealed class BodyView : MonoBehaviour
    {
        private Transform visual;
        private LineRenderer ring, telegraph, health, playerBeacon;
        private Renderer[] renderers;
        private MaterialPropertyBlock properties;
        private float flash;
        private BodyAnimation bodyAnimation;
        private Color[][] originalColors;
        private float markerHeight = 2.25f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public BodyState State { get; private set; }

        public void Initialize(BodyState state, Material material, Material lineMaterial)
        {
            State = state;
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            if (state.Definition.VisualPrefab != null) Instantiate(state.Definition.VisualPrefab, visual);
            else
            {
                var body = GameObject.CreatePrimitive(state.Definition.Kind == BodyKind.Brute ? PrimitiveType.Cube : state.Definition.Kind == BodyKind.Gunner ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(visual, false);
                body.transform.localPosition = Vector3.up * .8f;
                body.transform.localScale = state.Definition.Kind == BodyKind.Brute ? new Vector3(1.2f, 1.6f, 1) : state.Definition.Kind == BodyKind.Gunner ? new Vector3(.75f, 1.3f, .75f) : new Vector3(.65f, .8f, .65f);
                body.GetComponent<Renderer>().sharedMaterial = material;
                var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(weapon.GetComponent<Collider>());
                weapon.transform.SetParent(visual, false);
                weapon.transform.localPosition = new Vector3(.4f, .8f, .65f);
                weapon.transform.localScale = state.Definition.Kind == BodyKind.Gunner ? new Vector3(.18f,.18f,.85f) : new Vector3(state.Definition.Kind == BodyKind.Brute ? .45f : .14f, .15f, 1.1f);
                weapon.GetComponent<Renderer>().sharedMaterial = material;
            }
            renderers = visual.GetComponentsInChildren<Renderer>();
            bodyAnimation = visual.GetComponentInChildren<BodyAnimation>();
            if (bodyAnimation != null) bodyAnimation.Initialize(state);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                markerHeight = Mathf.Max(2.25f,bounds.max.y - transform.position.y + .3f);
            }
            originalColors = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                var materials = renderers[i].sharedMaterials;
                originalColors[i] = new Color[materials.Length];
                for (int j = 0; j < materials.Length; j++)
                    originalColors[i][j] = materials[j] != null && materials[j].HasProperty(BaseColor) ? materials[j].GetColor(BaseColor) : materials[j] != null && materials[j].HasProperty(ColorId) ? materials[j].GetColor(ColorId) : State.Definition.Color;
            }
            properties = new MaterialPropertyBlock();
            ring = MakeLine("Ownership / mark", lineMaterial, .055f);
            ring.loop = false; ring.positionCount = 41;
            telegraph = MakeLine("Attack telegraph", lineMaterial, .045f);
            telegraph.positionCount = 25;
            health = MakeLine("Remaining health", lineMaterial, .075f);
            health.positionCount = 2;
            playerBeacon = MakeLine("Player marker", lineMaterial, .1f);
            playerBeacon.positionCount = 3;
            playerBeacon.SetPositions(new[] { new Vector3(-.25f,markerHeight,0), new Vector3(0,markerHeight-.3f,0), new Vector3(.25f,markerHeight,0) });
            playerBeacon.startColor = playerBeacon.endColor = Color.cyan;
        }

        private LineRenderer MakeLine(string label, Material material, float width)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material; line.useWorldSpace = false;
            line.widthMultiplier = width; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        public void Refresh(GameSession session, bool paused = false)
        {
            if (visual == null) return;
            bool visible=session.IsVisible(State);
            foreach(var renderer in renderers) renderer.enabled=visible;
            transform.position = State.Position;
            // Telegraph stays in world-facing local coordinates; only the visual rotates.
            visual.rotation = Quaternion.LookRotation(State.Facing);
            if (bodyAnimation != null) bodyAnimation.Refresh(State,session.WorldScale,paused);
            bool marked = session.IsMarked(State);
            float markRemaining = session.MarkRemaining01(State);
            ring.enabled = visible && (State.IsPlayer || marked);
            float ringFraction = State.IsPlayer || session.Candidate == State ? 1 : markRemaining;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / (ring.positionCount - 1) * ringFraction;
                ring.SetPosition(i, new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)) * (State.Definition.Radius + .25f) + Vector3.up * .07f);
            }
            Color color = State.IsPlayer ? new Color(.3f, 1, 1) : session.Candidate == State ? Color.white : Color.Lerp(new Color(1,.55f,.1f), new Color(1,.25f,.7f), Mathf.Clamp01(markRemaining * 2));
            ring.startColor = ring.endColor = color;
            if (marked && session.Phase == RunPhase.Possessing && !session.CanPossess(State))
            { color.a = .25f; ring.startColor = ring.endColor = color; }
            ring.widthMultiplier = session.Candidate == State ? .13f + Mathf.Sin(Time.unscaledTime * 14) * .025f : State.IsPlayer ? .1f : .055f;
            playerBeacon.enabled = visible && State.IsPlayer && State.Alive;
            health.enabled = visible && !State.IsPlayer && State.Health < State.Definition.Health;
            health.startColor = health.endColor = marked ? new Color(1,.25f,.7f) : new Color(.8f,.85f,.9f);
            health.SetPosition(0, new Vector3(-.55f,markerHeight-.15f,0));
            health.SetPosition(1, new Vector3(-.55f + 1.1f * Mathf.Clamp01(State.Health / State.Definition.Health),markerHeight-.15f,0));
            telegraph.enabled = visible && State.Windup > 0 && !State.IsPlayer;
            if (telegraph.enabled)
            {
                telegraph.startColor = telegraph.endColor = new Color(1,.15f,.15f);
                float arc = State.Definition.Kind == BodyKind.Gunner ? 4 : State.Definition.Arc;
                telegraph.SetPosition(0, Vector3.up * .08f);
                for (int i = 1; i < 24; i++)
                {
                    float angle = -arc * .5f + arc * (i - 1) / 22;
                    telegraph.SetPosition(i, Quaternion.AngleAxis(angle, Vector3.up) * State.AttackDirection * State.Definition.Range + Vector3.up * .08f);
                }
                telegraph.SetPosition(24, Vector3.up * .08f);
            }
            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 7);
            for (int i = 0; i < renderers.Length; i++)
            {
                for (int j = 0; j < originalColors[i].Length; j++)
                {
                    Color baseColor = originalColors[i][j];
                    if (bodyAnimation != null)
                    {
                        baseColor = Color.Lerp(baseColor,State.Definition.Color,.18f);
                        baseColor = Color.Lerp(baseColor,Color.white,State.IsPlayer ? .18f : .08f);
                    }
                    Color tint = Color.Lerp(baseColor, Color.white, flash);
                    if (State.Invulnerable > 0 && State.IsPlayer) tint = Color.Lerp(tint,Color.white,.2f + Mathf.Sin(Time.unscaledTime * 32) * .15f);
                    renderers[i].GetPropertyBlock(properties,j);
                    properties.SetColor(BaseColor,tint); properties.SetColor(ColorId,tint);
                    renderers[i].SetPropertyBlock(properties,j);
                }
            }
        }

        public void React(CombatEvent message)
        {
            if (bodyAnimation != null) bodyAnimation.React(message);
            if (message.Cue == CombatCue.Death && bodyAnimation != null)
            {
                visual.DOKill(); visual.localScale = Vector3.one;
                var corpse = visual;
                corpse.SetParent(transform.parent,true);
                bodyAnimation.PlayDeath();
                corpse.DOScale(Vector3.zero,.18f).SetDelay(.9f).SetUpdate(true).SetLink(corpse.gameObject)
                    .OnComplete(() => { if (corpse != null) Destroy(corpse.gameObject); });
                visual = null;
                return;
            }
            if (message.Cue == CombatCue.Possessed)
            {
                flash = 1;
                visual.DOKill(); visual.localScale = Vector3.one;
                visual.DOPunchScale(new Vector3(.3f,.3f,.3f), .3f, 3, .3f).SetUpdate(true);
            }
            if (message.Cue == CombatCue.Hit)
            {
                flash = 1;
                visual.DOKill(); visual.localScale = Vector3.one;
                visual.DOPunchScale(new Vector3(.2f,-.15f,.2f), .14f, 4, .4f).SetUpdate(true);
            }
            if (message.Cue == CombatCue.Attack)
            {
                visual.DOKill(); visual.localScale = Vector3.one;
                visual.DOPunchScale(new Vector3(.1f,-.1f,.2f), .13f, 2, .2f).SetUpdate(true);
            }
        }
        private void OnDestroy() { if (visual != null) visual.DOKill(); }
    }
}
