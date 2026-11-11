using UnityEngine;

namespace SecondChance
{
    // Small generated clips, so missing audio files never leave an action silent.
    // Any of them can be replaced by assigning a real clip in CombatFeedback.
    public static class SynthAudio
    {
        public const int Rate = 44100;

        public static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Blade swing: band-limited noise that sweeps up and fades.
        public static AudioClip Whoosh()
        {
            var random = new System.Random(11);
            var data = new float[(int)(Rate * .17f)];
            float low = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                float noise = (float)random.NextDouble() * 2 - 1;
                low += (noise - low) * Mathf.Lerp(.06f, .45f, t);
                data[i] = low * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 1.6f) * .9f;
            }
            return Clip("Synth whoosh", data);
        }

        public static AudioClip Explosion()
        {
            var random = new System.Random(23);
            var data = new float[(int)(Rate * .8f)];
            float low = 0, phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)random.NextDouble() * 2 - 1;
                low += (noise - low) * Mathf.Lerp(.35f, .03f, Mathf.Clamp01(t * 2.5f));
                phase += (38 + 70 * Mathf.Exp(-t * 14)) / Rate;
                data[i] = Mathf.Clamp(low * Mathf.Exp(-t * 5.5f) * 1.6f + Mathf.Sin(phase * Mathf.PI * 2) * Mathf.Exp(-t * 7) * .8f, -1, 1);
            }
            return Clip("Synth explosion", data);
        }

        // Crate breaking: a few dry cracks.
        public static AudioClip Crack()
        {
            var random = new System.Random(37);
            var data = new float[(int)(Rate * .24f)];
            float previous = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)random.NextDouble() * 2 - 1;
                float bright = noise - previous * .6f; previous = noise;
                float envelope = Mathf.Exp(-t * 26) + .6f * Mathf.Exp(-Mathf.Abs(t - .06f) * 60) + .4f * Mathf.Exp(-Mathf.Abs(t - .12f) * 70);
                data[i] = Mathf.Clamp(bright * envelope * .55f, -1, 1);
            }
            return Clip("Synth crack", data);
        }

        // Possession-chain surge: quick rising triad.
        public static AudioClip Chime()
        {
            var data = new float[(int)(Rate * .6f)];
            float[] notes = { 880, 1108.73f, 1318.51f, 1760 };
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(n * .07f * Rate);
                for (int i = start; i < data.Length; i++)
                {
                    float t = (i - start) / (float)Rate;
                    data[i] += (Mathf.Sin(t * notes[n] * Mathf.PI * 2) + .35f * Mathf.Sin(t * notes[n] * Mathf.PI * 4)) * Mathf.Exp(-t * 9) * .22f;
                }
            }
            return Clip("Synth chime", data);
        }

        // Boss soul taking a new host: falling growl.
        public static AudioClip Growl()
        {
            var random = new System.Random(51);
            var data = new float[(int)(Rate * .75f)];
            float phase = 0, low = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                phase += Mathf.Lerp(150, 48, Mathf.Clamp01(t / .6f)) / Rate;
                float saw = (phase % 1) * 2 - 1;
                low += (saw - low) * .18f;
                float noise = (float)random.NextDouble() * 2 - 1;
                float envelope = Mathf.Min(1, t * 30) * Mathf.Exp(-t * 3.2f);
                data[i] = Mathf.Clamp((low * (1 + .5f * Mathf.Sin(t * 60)) + noise * .12f) * envelope * .8f, -1, 1);
            }
            return Clip("Synth growl", data);
        }
    }
}
