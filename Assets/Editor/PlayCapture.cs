using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CardGame.EditorTools
{
    // Editor-only harness: enters Play Mode, auto-plays the human seat (check/call)
    // so hands keep advancing, and renders the live camera to PNGs. Proves the game
    // actually runs and renders, without needing a device or emulator.
    public static class PlayCapture
    {
        private const int Width = 1080;
        private const int Height = 1920;

        private static string _dir;
        private static int _frame;
        private static int _shot;
        private static FieldInfo _tableField;

        public static void Run()
        {
            _dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots", "playmode");
            Directory.CreateDirectory(_dir);
            _frame = 0;
            _shot = 0;
            _tableField = typeof(PokerGame).GetField("_table", BindingFlags.NonPublic | BindingFlags.Instance);

            CIBuild.SetupScene();

            EditorApplication.update += OnUpdate;
            EditorApplication.EnterPlaymode();
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying) return; // still entering play mode

            _frame++;

            // Keep the game moving: when it stalls on the human, auto check/call.
            var game = Object.FindFirstObjectByType<PokerGame>();
            if (game != null && _tableField != null)
            {
                var table = _tableField.GetValue(game) as HorseTable;
                if (table != null && table.AwaitingHuman)
                {
                    var v = table.GetHumanOptions();
                    table.HumanAct(v.CanCheck ? ActionType.Check : ActionType.Call);
                }
            }

            // Capture spread across a few seconds of play.
            if (_frame == 20 || _frame == 90 || _frame == 170 || _frame == 260 || _frame == 360)
                Capture(game);

            if (_frame >= 380)
            {
                EditorApplication.update -= OnUpdate;
                Debug.Log($"PlayCapture: CAPTURE_OK shots={_shot} dir={_dir}");
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(0);
            }
        }

        private static void Capture(PokerGame game)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var rt = new RenderTexture(Width, Height, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            string path = Path.Combine(_dir, $"play_{_shot:D2}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"PlayCapture: wrote {path}");
            _shot++;

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
