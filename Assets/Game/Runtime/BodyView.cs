using DG.Tweening;
using UnityEngine;

namespace SecondChance
{
    public sealed class BodyView : MonoBehaviour
    {
        private Transform visual;
        private LineRenderer ring, telegraph, health;
        private Renderer[] renderers;
        private MaterialPropertyBlock properties;
        private float flash;
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
            properties = new MaterialPropertyBlock();
            ring = MakeLine("Ownership / mark", lineMaterial, .055f);
            ring.loop = true; ring.positionCount = 40;
            for (int i = 0; i < 40; i++) { float angle = i * Mathf.PI * 2 / 40; ring.SetPosition(i, new Vector3(Mathf.Cos(angle), .06f, Mathf.Sin(angle)) * (state.Definition.Radius + .25f)); }
            telegraph = MakeLine("Attack telegraph", lineMaterial, .045f);
            telegraph.positionCount = 25;
            health = MakeLine("Remaining health", lineMaterial, .075f);
            health.positionCount = 2;
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

        public void Refresh(GameSession session)
        {
            transform.position = State.Position;
            // Telegraph stays in world-facing local coordinates; only the visual rotates.
            visual.rotation = Quaternion.LookRotation(State.Facing);
            bool marked = session.IsMarked(State);
            ring.enabled = State.IsPlayer || marked;
            Color color = State.IsPlayer ? new Color(.3f, 1, 1) : session.Candidate == State ? Color.white : new Color(1,.25f,.7f);
            ring.startColor = ring.endColor = color;
            if (marked && session.Phase == RunPhase.Possessing && !session.CanPossess(State))
            { color.a = .25f; ring.startColor = ring.endColor = color; }
            ring.widthMultiplier = session.Candidate == State ? .13f + Mathf.Sin(Time.unscaledTime * 14) * .025f : .055f;
            health.enabled = !State.IsPlayer && State.Health < State.Definition.Health;
            health.startColor = health.endColor = marked ? new Color(1,.25f,.7f) : new Color(.8f,.85f,.9f);
            health.SetPosition(0, new Vector3(-.55f,2,0));
            health.SetPosition(1, new Vector3(-.55f + 1.1f * Mathf.Clamp01(State.Health / State.Definition.Health),2,0));
            telegraph.enabled = State.Windup > 0 && !State.IsPlayer;
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
            Color tint = Color.Lerp(State.Definition.Color, Color.white, flash);
            if (State.Invulnerable > 0 && State.IsPlayer) tint = Color.Lerp(tint, Color.white, .25f + Mathf.Sin(Time.unscaledTime * 32) * .2f);
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetColor(BaseColor, tint); properties.SetColor(ColorId, tint);
                renderer.SetPropertyBlock(properties);
            }
        }

        public void React(CombatEvent message)
        {
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
