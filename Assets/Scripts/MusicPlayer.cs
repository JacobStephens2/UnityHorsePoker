using UnityEngine;

namespace CardGame
{
    // Procedurally synthesizes a soft, looping lounge/casino backing track at runtime — no audio assets needed.
    // A I-vi-ii-V progression (Cmaj7 - Am7 - Dm7 - G7) with a swelling pad, pulsing bass, and a gentle arpeggio.
    public class MusicPlayer : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float ChordSeconds = 2.2f;

        private static readonly int[][] Chords =
        {
            new[] { 60, 64, 67, 71 }, // Cmaj7
            new[] { 57, 60, 64, 67 }, // Am7
            new[] { 62, 65, 69, 72 }, // Dm7
            new[] { 55, 59, 62, 65 }  // G7
        };
        private static readonly int[] Bass = { 48, 45, 50, 43 }; // C3 A2 D3 G2

        public bool Muted;
        private AudioSource _src;

        private void Start()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = Build();
            _src.loop = true;
            _src.volume = 0.30f;
            _src.spatialBlend = 0f;
            _src.Play();
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            if (_src != null) _src.volume = Muted ? 0f : 0.30f;
        }

        private AudioClip Build()
        {
            int chordLen = (int)(SampleRate * ChordSeconds);
            int total = chordLen * Chords.Length;
            var data = new float[total];
            int stepsPerChord = 4;
            int stepLen = chordLen / stepsPerChord;

            float peak = 0f;
            for (int c = 0; c < Chords.Length; c++)
            {
                int baseI = c * chordLen;
                float bassF = Note(Bass[c]);
                var notes = Chords[c];

                for (int i = 0; i < chordLen; i++)
                {
                    float t = (baseI + i) / (float)SampleRate;
                    // Smooth swell: 0 at chord edges, peak in the middle -> seamless, click-free loop.
                    float env = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * (i / (float)chordLen)));

                    float pad = 0f;
                    for (int n = 0; n < notes.Length; n++)
                        pad += Mathf.Sin(2f * Mathf.PI * Note(notes[n]) * t);
                    pad *= 0.05f * env;

                    float bass = Mathf.Sin(2f * Mathf.PI * bassF * t) * 0.16f * env;

                    // Gentle arpeggio one octave up, plucked.
                    int step = i / stepLen;
                    float localT = (i - step * stepLen) / (float)SampleRate;
                    float arpF = Note(notes[step % notes.Length] + 12);
                    float arp = Mathf.Sin(2f * Mathf.PI * arpF * t) * 0.10f * Mathf.Exp(-localT * 5f) * env;

                    float s = pad + bass + arp;
                    data[baseI + i] = s;
                    float a = Mathf.Abs(s);
                    if (a > peak) peak = a;
                }
            }

            // Normalize to a safe headroom.
            if (peak > 0.0001f)
            {
                float g = 0.85f / peak;
                for (int i = 0; i < total; i++) data[i] *= g;
            }

            var clip = AudioClip.Create("bgm", total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Note(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }
}
