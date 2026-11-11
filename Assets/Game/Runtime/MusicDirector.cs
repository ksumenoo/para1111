using UnityEngine;

namespace SecondChance
{
    // Generated four-layer loop (pad / bass / drums / lead). Layers fade with the state of the fight,
    // so the music follows the action without any audio files. Presentation only.
    public sealed class MusicDirector : MonoBehaviour
    {
        [Range(0,1)] public float Volume = .3f;
        public bool Paused;
        private void OnDestroy()
        {
            foreach(var layer in layers)
                if(layer != null) { layer.Stop(); if(layer.clip != null) Destroy(layer.clip); }
        }

        private const float Bpm = 112;
        private const int Bars = 4, Steps = Bars * 16;
        private static readonly float[] LayerGain = { .22f, .24f, .27f, .17f };
        // Am - F - C - G: root, third, fifth.
        private static readonly float[][] Chords =
        {
            new[] { 110f, 130.81f, 164.81f },
            new[] { 87.31f, 110f, 130.81f },
            new[] { 130.81f, 164.81f, 196f },
            new[] { 98f, 123.47f, 146.83f }
        };
        private readonly AudioSource[] layers = new AudioSource[4];
        private readonly float[] levels = new float[4];
        private GameSession session;
        private AudioLowPassFilter filter;
        private int samples;

        public void Initialize(GameSession game)
        {
            session = game;
            samples = Mathf.RoundToInt(SynthAudio.Rate * 60f / Bpm * 4 * Bars);
            var host = new GameObject("Generated music");
            host.transform.SetParent(transform, false);
            AudioClip[] clips = { SynthAudio.Clip("Music pad", Pad()), SynthAudio.Clip("Music bass", Bass()), SynthAudio.Clip("Music drums", Drums()), SynthAudio.Clip("Music lead", Lead()) };
            double start = AudioSettings.dspTime + .2;
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = host.AddComponent<AudioSource>();
                layers[i].clip = clips[i]; layers[i].loop = true; layers[i].playOnAwake = false;
                layers[i].spatialBlend = 0; layers[i].volume = 0; layers[i].priority = 200;
                layers[i].PlayScheduled(start);
            }
            filter = host.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = 22000;
        }

        private void Update()
        {
            if (session == null || layers[0] == null) return;
            float pad = 1, bass = 0, drums = 0, lead = 0, cutoff = 22000, pitch = 1;
            switch (session.Phase)
            {
                case RunPhase.Intermission: bass = .8f; break;
                case RunPhase.Fighting:
                    int enemies = 0;
                    for (int i = 0; i < session.Bodies.Count; i++) if (session.Bodies[i].Alive && !session.Bodies[i].IsPlayer) enemies++;
                    pad = .7f; bass = 1; drums = 1;
                    lead = session.BossFight || session.Surging ? 1 : enemies >= 6 ? .7f : 0;
                    break;
                case RunPhase.Possessing: pad = 1; bass = .5f; cutoff = 700; pitch = .82f; break;
                case RunPhase.Won: lead = .8f; bass = .5f; break;
                case RunPhase.Lost: cutoff = 900; pitch = .9f; break;
            }
            if (Paused) cutoff = Mathf.Min(cutoff, 1000);
            float[] targets = { pad, bass, drums, lead };
            float step = Time.unscaledDeltaTime * 1.6f;
            for (int i = 0; i < layers.Length; i++)
            {
                levels[i] = Mathf.MoveTowards(levels[i], targets[i], step);
                layers[i].volume = levels[i] * LayerGain[i] * Mathf.Clamp01(Volume);
                layers[i].pitch = Mathf.MoveTowards(layers[i].pitch, pitch, Time.unscaledDeltaTime * 1.5f);
            }
            filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, cutoff, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 8));
        }

        private int StepStart(int step) => (int)((long)samples * step / Steps);

        private float[] Pad()
        {
            var data = new float[samples];
            int bar = samples / Bars;
            for (int i = 0; i < samples; i++)
            {
                int index = Mathf.Min(Bars - 1, i / bar);
                float t = i / (float)SynthAudio.Rate, local = (i - index * bar) / (float)bar;
                // Fade each chord at the bar edges, which also makes the loop point silent and click-free.
                float envelope = Mathf.Pow(Mathf.Sin(local * Mathf.PI), .45f);
                float sum = 0;
                foreach (float note in Chords[index])
                    sum += Mathf.Sin(t * note * Mathf.PI * 2) + .6f * Mathf.Sin(t * note * 1.006f * Mathf.PI * 2) + .25f * Mathf.Sin(t * note * 2 * Mathf.PI * 2);
                data[i] = sum * envelope * .075f;
            }
            return data;
        }

        private float[] Bass()
        {
            var data = new float[samples];
            // Eighth-note pulse with two rests per bar.
            bool[] pattern = { true, true, false, true, true, false, true, true };
            for (int step = 0; step < Steps; step += 2)
            {
                if (!pattern[step / 2 % 8]) continue;
                float note = Chords[step / 16][0] * .5f;
                int start = StepStart(step), end = Mathf.Min(samples, StepStart(step + 2));
                for (int i = start; i < end; i++)
                {
                    float t = (i - start) / (float)SynthAudio.Rate, phase = t * note * Mathf.PI * 2;
                    float wave = Mathf.Sin(phase) + .45f * Mathf.Sin(phase * 2) + .22f * Mathf.Sin(phase * 3);
                    float tail = Mathf.Clamp01((end - i) / 300f);
                    data[i] = wave * Mathf.Min(1, t * 200) * Mathf.Exp(-t * 7) * tail * .5f;
                }
            }
            return data;
        }

        private float[] Drums()
        {
            var data = new float[samples];
            var random = new System.Random(7);
            for (int step = 0; step < Steps; step++)
            {
                int start = StepStart(step);
                int inBar = step % 16;
                bool kick = inBar % 4 == 0 || inBar == 10, snare = inBar == 4 || inBar == 12, hat = inBar % 2 == 0, ghost = inBar % 2 == 1;
                float phase = 0, previous = 0;
                int length = Mathf.Min(samples - start, (int)(SynthAudio.Rate * .28f));
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)SynthAudio.Rate, value = 0;
                    float noise = (float)random.NextDouble() * 2 - 1;
                    float bright = noise - previous; previous = noise;
                    if (kick) { phase += (46 + 95 * Mathf.Exp(-t * 32)) / SynthAudio.Rate; value += Mathf.Sin(phase * Mathf.PI * 2) * Mathf.Exp(-t * 10) * .85f; }
                    if (snare) value += noise * Mathf.Exp(-t * 24) * .32f + Mathf.Sin(t * 190 * Mathf.PI * 2) * Mathf.Exp(-t * 32) * .2f;
                    if (hat) value += bright * Mathf.Exp(-t * 75) * .12f;
                    if (ghost) value += bright * Mathf.Exp(-t * 110) * .05f;
                    data[start + i] += value;
                }
            }
            for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(data[i], -1, 1);
            return data;
        }

        private float[] Lead()
        {
            var data = new float[samples];
            int[] order = { 0, 2, 1, 2, 3, 2, 1, 2 };
            for (int step = 0; step < Steps; step++)
            {
                var chord = Chords[step / 16];
                int pick = order[step % 8];
                float note = (pick == 3 ? chord[0] * 2 : chord[pick]) * 4;
                int start = StepStart(step), length = Mathf.Min(samples - start, (int)(SynthAudio.Rate * .2f));
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)SynthAudio.Rate, phase = t * note * Mathf.PI * 2;
                    float wave = Mathf.Sin(phase) + .33f * Mathf.Sin(phase * 3) + .2f * Mathf.Sin(phase * 5);
                    data[start + i] += wave * Mathf.Min(1, t * 400) * Mathf.Exp(-t * 16) * .22f;
                }
            }
            return data;
        }
    }
}
