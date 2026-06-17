using UnityEngine;

namespace CardGame
{
    // Core "Higher or Lower" game loop. Builds the play field, handles touch input and scoring.
    public class GameController : MonoBehaviour
    {
        private Deck _deck;
        private Card _current;
        private CardView _cardView;
        private TouchButton _higher;
        private TouchButton _lower;
        private TextMesh _scoreText;
        private TextMesh _resultText;

        private int _score;
        private int _streak;
        private int _best;

        private void Start()
        {
            _deck = new Deck();
            BuildField();
            _current = _deck.Draw();
            _cardView.Show(_current);
            UpdateHud("Higher or Lower?");
        }

        private void BuildField()
        {
            // Lay everything out relative to the camera's real world bounds so it fits
            // any aspect ratio (wide editor Game view AND tall phone screens).
            var cam = Camera.main;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // Card centred, in the upper third.
            var cardGo = new GameObject("Card");
            cardGo.transform.position = new Vector3(0f, halfH * 0.30f, 0f);
            _cardView = cardGo.AddComponent<CardView>();
            _cardView.Init();

            // Buttons along the bottom — width and X are derived from the screen so they
            // never run off the edges on a narrow portrait device.
            const float nativeBtnW = 2.4f;
            float btnW = Mathf.Min(nativeBtnW, halfW * 0.88f);
            float btnScale = btnW / nativeBtnW;
            float btnX = halfW - btnW * 0.5f - halfW * 0.04f;
            float btnY = -halfH + 1.4f;

            _lower = MakeButton("LowerButton", "LOWER", new Vector3(-btnX, btnY, 0f), new Color(0.20f, 0.45f, 0.85f), btnScale);
            _higher = MakeButton("HigherButton", "HIGHER", new Vector3(btnX, btnY, 0f), new Color(0.20f, 0.65f, 0.35f), btnScale);
            _lower.Clicked += () => Guess(false);
            _higher.Clicked += () => Guess(true);

            _scoreText = TextFactory.Create("Score", null, new Vector3(0f, halfH - 0.8f, 0f), 50, Color.white);
            _resultText = TextFactory.Create("Result", null, new Vector3(0f, -halfH * 0.18f, 0f), 40, new Color(0.9f, 0.9f, 0.6f));
        }

        private TouchButton MakeButton(string name, string label, Vector3 pos, Color color, float scale)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var btn = go.AddComponent<TouchButton>();
            btn.Init(label, color);
            return btn;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
                if (hit != null)
                {
                    var btn = hit.GetComponent<TouchButton>();
                    if (btn != null) btn.Press();
                }
            }
        }

        private void Guess(bool higher)
        {
            Card next = _deck.Draw();
            bool correct = higher ? next.Rank > _current.Rank : next.Rank < _current.Rank;

            _current = next;
            _cardView.Show(_current);

            if (correct)
            {
                _score++;
                _streak++;
                _best = Mathf.Max(_best, _streak);
                UpdateHud("Correct! Streak " + _streak);
            }
            else
            {
                _streak = 0;
                UpdateHud("Nope! Streak reset");
            }
        }

        private void UpdateHud(string message)
        {
            _scoreText.text = "Score " + _score + "    Best " + _best;
            _resultText.text = message;
        }
    }
}
