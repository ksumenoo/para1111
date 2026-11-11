using DG.Tweening;
using UnityEngine;

namespace SecondChance
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private AudioClip hitSound, deathSound, dashSound, transferSound, attackSound;
        [SerializeField] private AudioClip heavyHitSound, shotSound, enemyDeathSound;
        [SerializeField, Range(0,1)] private float masterVolume = .65f;
        [SerializeField] private Texture2D sparkTexture, muzzleTexture;
        private FollowCameraRig cameraRig;
        private Material lineMaterial;
        // Separate fixed voices prevent crowd kills from multiplying overlapping OneShots.
        // Bus gains sum to 1, leaving headroom at the configured master volume.
        private readonly AudioSource[] voices = new AudioSource[4];
        private readonly float[] nextSoundTime = new float[4];
        private static readonly float[] BusGain = { .36f, .1f, .14f, .4f };
        private float shake;
        private Material sparkMaterial, muzzleMaterial;
        private bool visibleEffect = true;

        public void Initialize(Camera camera, Material material)
        {
            cameraRig = GetComponent<FollowCameraRig>(); lineMaterial = material;
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].spatialBlend = 0; voices[i].playOnAwake = false;
                voices[i].priority = i == 3 ? 64 : 128;
            }
            if (sparkTexture != null) sparkMaterial = new Material(material) { mainTexture = sparkTexture };
            if (muzzleTexture != null) muzzleMaterial = new Material(material) { mainTexture = muzzleTexture };
        }

        public void Play(CombatEvent message, bool visible = true)
        {
            visibleEffect=visible;
            AudioClip clip = null;
            float volume = 1;
            int channel = 0;
            float spacing = .055f;
            switch (message.Cue)
            {
                case CombatCue.Hit:
                    if(visible) shake = Mathf.Max(shake, message.Body.IsPlayer ? .28f : .12f);
                    Burst(message.Position, Color.white, .6f, .12f);
                    Sparks(message.Position, message.Body.IsPlayer ? new Color(1,.2f,.4f) : Color.cyan, 5, sparkMaterial);
                    clip = message.Other != null && message.Other.Definition.Kind == BodyKind.Brute && heavyHitSound != null ? heavyHitSound : hitSound;
                    volume = 1; break;
                case CombatCue.Death:
                    if(visible) shake = Mathf.Max(shake, message.Body.IsPlayer ? .6f : .25f);
                    Burst(message.Position, message.Body.Definition.Color, 1.5f, .32f);
                    Sparks(message.Position, message.Body.IsPlayer ? Color.cyan : message.Body.Definition.Color, 12, sparkMaterial);
                    clip = message.Body.IsPlayer ? deathSound : enemyDeathSound;
                    channel = message.Body.IsPlayer ? 3 : 1;
                    spacing = message.Body.IsPlayer ? 0 : .12f;
                    volume = message.Body.IsPlayer ? 1 : .65f;
                    if (message.Body.IsPlayer) for (int i = 0; i < 3; i++) voices[i].Stop();
                    break;
                case CombatCue.Dash:
                    Burst(message.Position, Color.cyan, .9f, .18f); clip = dashSound; volume = .65f; channel = 3; spacing = .06f; break;
                case CombatCue.Possessed:
                    shake = .35f;
                    Burst(message.Position, Color.cyan, 2, .4f); clip = transferSound; channel = 3; spacing = 0; break;
                case CombatCue.Attack:
                    bool gunner = message.Body.Definition.Kind == BodyKind.Gunner;
                    if (!gunner) Slash(message);
                    else Sparks(message.Position + message.Direction * .9f, Color.cyan, 2, muzzleMaterial);
                    clip = gunner ? shotSound : attackSound;
                    volume = message.Body.IsPlayer ? (gunner ? .8f : .45f) : .25f;
                    channel = 2; spacing = .045f; break;
            }
            PlayVoice(clip, volume, channel, spacing);
        }

        private void PlayVoice(AudioClip clip, float volume, int channel, float spacing)
        {
            if (clip == null || Time.unscaledTime < nextSoundTime[channel]) return;
            var voice = voices[channel];
            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(masterVolume) * BusGain[channel] * Mathf.Clamp01(volume);
            voice.pitch = Random.Range(.97f,1.03f);
            voice.Play();
            nextSoundTime[channel] = Time.unscaledTime + spacing;
        }

        private void Sparks(Vector3 position, Color color, int count, Material material)
        {
            if (!visibleEffect || material == null) return;
            var particles = new GameObject("Electric impact").AddComponent<ParticleSystem>();
            particles.transform.SetParent(transform, false); particles.transform.position = position + Vector3.up * .8f;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false; main.playOnAwake = false; main.duration = .4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.12f,.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f,4);
            main.startSize = new ParticleSystem.MinMaxCurve(.2f,.55f);
            main.startColor = color; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true; main.maxParticles = 24;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .1f;
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) }, new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(0,1) });
            fade.color = gradient;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            particles.Play(); particles.Emit(count);
            Destroy(particles.gameObject, .8f);
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
            if(!visibleEffect)return;
            var line = new GameObject("Combat FX").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = lineMaterial; line.positionCount = points.Length;
            line.SetPositions(points); line.startColor = line.endColor = color; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DOTween.To(() => width, value => { if (line != null) line.widthMultiplier = value; }, 0, duration)
                .SetUpdate(true).SetTarget(line).OnComplete(() => { if (line != null) Destroy(line.gameObject); });
        }

        private void Update()
        {
            shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 2.8f);
            Vector2 offset = Random.insideUnitCircle * shake;
            if (cameraRig != null) cameraRig.ShakeOffset = new Vector3(offset.x, offset.y, 0);
        }

        private void OnDestroy()
        {
            foreach (var line in GetComponentsInChildren<LineRenderer>()) DOTween.Kill(line);
            if (sparkMaterial != null) Destroy(sparkMaterial);
            if (muzzleMaterial != null) Destroy(muzzleMaterial);
        }
    }
}
