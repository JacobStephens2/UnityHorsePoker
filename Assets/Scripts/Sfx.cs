using UnityEngine;

namespace CardGame
{
    // Procedurally synthesized sound effects (no audio assets). One shared AudioSource plays short generated clips.
    public static class Sfx
    {
        private const int SR = 44100;
        private static AudioSource _src;
        private static AudioClip _click, _chip, _deal, _win, _fold;
        private static System.Random _rng = new System.Random(1);
        public static bool Muted;

        public static void Init(GameObject host)
        {
            _src = host.AddComponent<AudioSource>();
            _src.spatialBlend = 0f;
            _src.playOnAwake = false;

            _click = Tone("click", 0.06f, 40f, t => Mathf.Sin(2 * Mathf.PI * 900 * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * 1800 * t));
            _chip = Noise("chip", 0.10f, 28f, 0.5f, 1300f);
            _deal = Noise("deal", 0.13f, 16f, 0.7f, 0f);
            _fold = Tone("fold", 0.25f, 11f, t => Mathf.Sin(2 * Mathf.PI * 150 * t));
            _win = Chime("win", new[] { 72, 76, 79, 84 }); // C5 E5 G5 C6
        }

        public static void Click() => Play(_click, 0.5f);
        public static void Chip() => Play(_chip, 0.6f);
        public static void Deal() => Play(_deal, 0.5f);
        public static void Fold() => Play(_fold, 0.5f);
        public static void Win() => Play(_win, 0.6f);

        private static void Play(AudioClip c, float vol)
        {
            if (_src != null && c != null && !Muted) _src.PlayOneShot(c, vol);
        }

        private static AudioClip Tone(string name, float dur, float decay, System.Func<float, float> wave)
        {
            int len = (int)(SR * dur);
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SR;
                d[i] = wave(t) * Mathf.Exp(-t * decay) * 0.6f;
            }
            return Make(name, d);
        }

        // Noise burst with optional tonal "tick" mixed in (for chip/card sounds).
        private static AudioClip Noise(string name, float dur, float decay, float toneMix, float toneFreq)
        {
            int len = (int)(SR * dur);
            var d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * decay);
                float n = (float)(_rng.NextDouble() * 2.0 - 1.0);
                float tone = toneFreq > 0 ? Mathf.Sin(2 * Mathf.PI * toneFreq * t) * toneMix : 0f;
                d[i] = (n * (1f - toneMix) + tone) * env * 0.6f;
            }
            return Make(name, d);
        }

        // Staggered bell-like ascending notes.
        private static AudioClip Chime(string name, int[] midi)
        {
            float noteDur = 0.16f, gap = 0.10f;
            int len = (int)(SR * (gap * midi.Length + noteDur));
            var d = new float[len];
            for (int k = 0; k < midi.Length; k++)
            {
                float f = 440f * Mathf.Pow(2f, (midi[k] - 69) / 12f);
                int start = (int)(SR * gap * k);
                int nlen = (int)(SR * noteDur);
                for (int i = 0; i < nlen && start + i < len; i++)
                {
                    float t = i / (float)SR;
                    float env = Mathf.Exp(-t * 9f);
                    d[start + i] += (Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * f * 2 * t)) * env * 0.35f;
                }
            }
            return Make(name, d);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
