using DG.Tweening;
using UnityEngine;

namespace SecondChance
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private AudioClip hitSound, deathSound, dashSound, transferSound, attackSound;
        private Camera gameCamera;
        private Vector3 cameraRest;
        private Material lineMaterial;
        private AudioSource audioSource;
        private float shake;

        public void Initialize(Camera camera, Material material)
        {
            gameCamera = camera; cameraRest = camera.transform.position; lineMaterial = material;
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend = 0;
        }

        public void Play(CombatEvent message)
        {
            AudioClip clip = null;
            switch (message.Cue)
            {
                case CombatCue.Hit:
                    shake = Mathf.Max(shake, message.Body.IsPlayer ? .28f : .12f);
                    Burst(message.Position, Color.white, .6f, .12f); clip = hitSound; break;
                case CombatCue.Death:
                    shake = Mathf.Max(shake, message.Body.IsPlayer ? .6f : .25f);
                    Burst(message.Position, message.Body.Definition.Color, 1.5f, .32f); clip = deathSound; break;
                case CombatCue.Dash:
                    Burst(message.Position, Color.cyan, .9f, .18f); clip = dashSound; break;
                case CombatCue.Possessed:
                    shake = .35f;
                    DrawLine(new[] { message.Other.Position + Vector3.up, message.Position + Vector3.up }, Color.cyan, .22f, .3f);
                    Burst(message.Position, Color.cyan, 2, .4f); clip = transferSound; break;
                case CombatCue.Attack:
                    if (message.Body.Definition.Kind != BodyKind.Gunner) Slash(message);
                    clip = attackSound; break;
            }
            if (clip != null) { audioSource.pitch = Random.Range(.94f,1.06f); audioSource.PlayOneShot(clip, .8f); }
        }

        private void Slash(CombatEvent message)
        {
            var points = new Vector3[18];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.Lerp(-message.Body.Definition.Arc * .5f, message.Body.Definition.Arc * .5f, i / 17f);
                points[i] = message.Position + Vector3.up * .7f + Quaternion.AngleAxis(angle, Vector3.up) * message.Direction * message.Body.Definition.Range;
            }
            DrawLine(points, message.Body.IsPlayer ? Color.white : new Color(1,.25f,.1f), .12f, .13f);
        }

        private void Burst(Vector3 origin, Color color, float radius, float duration)
        {
            var points = new Vector3[33];
            for (int i = 0; i < points.Length; i++) { float angle = i * Mathf.PI * 2 / 32; points[i] = origin + new Vector3(Mathf.Cos(angle), .15f, Mathf.Sin(angle)) * radius; }
            DrawLine(points, color, .12f, duration);
        }

        private void DrawLine(Vector3[] points, Color color, float width, float duration)
        {
            var line = new GameObject("Combat FX").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = lineMaterial; line.positionCount = points.Length;
            line.SetPositions(points); line.startColor = line.endColor = color; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DOTween.To(() => width, value => { if (line != null) line.widthMultiplier = value; }, 0, duration)
                .SetUpdate(true).SetTarget(line).OnComplete(() => { if (line != null) Destroy(line.gameObject); });
        }

        private void LateUpdate()
        {
            if (gameCamera == null) return;
            shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 2.8f);
            Vector2 offset = Random.insideUnitCircle * shake;
            gameCamera.transform.position = cameraRest + new Vector3(offset.x, offset.y, 0);
        }

        private void OnDestroy()
        {
            foreach (var line in GetComponentsInChildren<LineRenderer>()) DOTween.Kill(line);
            if (gameCamera != null) gameCamera.transform.position = cameraRest;
        }
    }
}
