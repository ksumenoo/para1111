using DG.Tweening;
using UnityEngine;

namespace SecondChance
{
    // Presentation only: targeting and the two-second deadline remain owned by GameSession.
    public sealed class PossessionView : MonoBehaviour
    {
        private GameSession session;
        private Transform soulRoot, orb;
        private LineRenderer radius, countdown, tether;
        private Material soulMaterial;
        private Tween transfer;
        private GameObject travellingSoul;
        private float pulse;
        private const int Segments = 64;
        private static readonly Color Cyan = new Color(.15f, 1, 1);
        private static readonly Color Pink = new Color(1, .2f, .55f);

        public void Initialize(GameSession game, Material lineMaterial)
        {
            session = game;
            soulMaterial = new Material(lineMaterial) { color = Cyan };
            soulRoot = new GameObject("Soul / capture radius").transform;
            soulRoot.SetParent(transform, false);
            orb = MakeOrb("Exposed soul", soulRoot);
            radius = Line("Capture range", soulRoot, lineMaterial, .045f, Segments + 1);
            countdown = Line("Real-time deadline", soulRoot, lineMaterial, .13f, Segments + 1);
            tether = Line("Selected host link", soulRoot, lineMaterial, .07f, 24);
            soulRoot.gameObject.SetActive(false);
            session.Combat += OnCombat;
        }

        public void Refresh(bool paused)
        {
            if (session == null) return;
            bool choosing = session.Phase == RunPhase.Possessing && session.Player != null;
            soulRoot.gameObject.SetActive(choosing);
            if (!choosing) return;
            if (!paused) pulse += Time.unscaledDeltaTime;
            soulRoot.position = session.Player.Position;
            float remaining = Mathf.Clamp01(session.PossessionRemaining / session.Settings.PossessionSeconds);
            Color urgency = Color.Lerp(Pink, Cyan, Mathf.Clamp01(remaining * 2));
            orb.localPosition = Vector3.up * (1.15f + Mathf.Sin(pulse * 9) * .12f);
            orb.localScale = Vector3.one * (.38f + Mathf.Sin(pulse * 13) * .045f);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2 / Segments;
                radius.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * session.Settings.PossessionRadius + Vector3.up * .09f);
                float clockAngle = i * Mathf.PI * 2 / Segments * remaining;
                countdown.SetPosition(i, new Vector3(Mathf.Sin(clockAngle), .15f, Mathf.Cos(clockAngle)) * 1.1f);
            }
            Color rangeColor = urgency; rangeColor.a = .35f;
            radius.startColor = radius.endColor = rangeColor;
            countdown.startColor = countdown.endColor = urgency;
            tether.enabled = session.Candidate != null;
            if (!tether.enabled) return;
            Vector3 target = session.Candidate.Position - soulRoot.position + Vector3.up * 1.15f;
            for (int i = 0; i < tether.positionCount; i++)
            {
                float t = i / (float)(tether.positionCount - 1);
                Vector3 point = Vector3.Lerp(orb.localPosition, target, t);
                point.y += Mathf.Sin(t * Mathf.PI) * (.5f + Mathf.Sin(pulse * 10 + t * 8) * .08f);
                tether.SetPosition(i, point);
            }
            tether.startColor = urgency; tether.endColor = Color.white;
            tether.widthMultiplier = .065f + Mathf.Sin(pulse * 12) * .018f;
        }

        private void OnCombat(CombatEvent message)
        {
            if (message.Cue != CombatCue.Possessed || message.Other == null) return;
            ResetTransfer();
            travellingSoul = new GameObject("Soul transfer");
            travellingSoul.transform.SetParent(transform, false);
            var spark = MakeOrb("Transfer spark", travellingSoul.transform);
            spark.localScale = Vector3.one * .45f;
            var trail = travellingSoul.AddComponent<TrailRenderer>();
            trail.sharedMaterial = soulMaterial; trail.time = .16f; trail.minVertexDistance = .03f;
            trail.startWidth = .35f; trail.endWidth = 0;
            trail.startColor = Cyan; trail.endColor = new Color(.15f,1,1,0);
            Vector3 start = message.Other.Position + Vector3.up * 1.15f;
            travellingSoul.transform.position = start;
            var host = message.Body;
            float progress = 0;
            transfer = DOTween.To(() => progress, value =>
            {
                progress = value;
                if (travellingSoul != null)
                    travellingSoul.transform.position = Vector3.Lerp(start, host.Position + Vector3.up * .8f, value) + Vector3.up * Mathf.Sin(value * Mathf.PI) * 1.2f;
            }, 1, .22f).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() =>
            {
                if (travellingSoul != null) Destroy(travellingSoul);
                travellingSoul = null; transfer = null;
            });
        }

        public void ResetVisuals()
        {
            ResetTransfer(); pulse = 0;
            if (soulRoot != null) soulRoot.gameObject.SetActive(false);
        }

        private void ResetTransfer()
        {
            transfer?.Kill(); transfer = null;
            if (travellingSoul != null) Destroy(travellingSoul);
            travellingSoul = null;
        }

        private Transform MakeOrb(string name, Transform parent)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            result.name = name; Destroy(result.GetComponent<Collider>());
            result.transform.SetParent(parent, false);
            result.transform.localScale = Vector3.one * .4f;
            result.GetComponent<Renderer>().sharedMaterial = soulMaterial;
            return result.transform;
        }

        private static LineRenderer Line(string name, Transform parent, Material material, float width, int count)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false); line.useWorldSpace = false;
            line.sharedMaterial = material; line.widthMultiplier = width; line.positionCount = count;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        private void OnDestroy()
        {
            if (session != null) session.Combat -= OnCombat;
            ResetTransfer();
            if (soulRoot != null) Destroy(soulRoot.gameObject);
            if (soulMaterial != null) Destroy(soulMaterial);
        }
    }
}
