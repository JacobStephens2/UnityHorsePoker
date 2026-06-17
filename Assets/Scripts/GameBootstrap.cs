using UnityEngine;

namespace CardGame
{
    // Sole component placed in the scene. Configures the 2D camera and starts the game.
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var camGo = Camera.main;
            if (camGo == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camGo = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            camGo.orthographic = true;
            camGo.orthographicSize = 5.5f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.clearFlags = CameraClearFlags.SolidColor;
            camGo.backgroundColor = new Color(0.10f, 0.27f, 0.20f); // card-table green

            gameObject.AddComponent<MusicPlayer>();
            gameObject.AddComponent<PokerGame>();
        }
    }
}
