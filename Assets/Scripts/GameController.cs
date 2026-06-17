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
            // Card in the centre-top.
            var cardGo = new GameObject("Card");
            cardGo.transform.position = new Vector3(0f, 1.4f, 0f);
            _cardView = cardGo.AddComponent<CardView>();
            _cardView.Init();

            // Buttons along the bottom.
            _lower = MakeButton("LowerButton", "LOWER", new Vector3(-1.6f, -2.6f, 0f), new Color(0.20f, 0.45f, 0.85f));
            _higher = MakeButton("HigherButton", "HIGHER", new Vector3(1.6f, -2.6f, 0f), new Color(0.20f, 0.65f, 0.35f));
            _lower.Clicked += () => Guess(false);
            _higher.Clicked += () => Guess(true);

            _scoreText = TextFactory.Create("Score", null, new Vector3(0f, 3.9f, 0f), 50, Color.white);
            _resultText = TextFactory.Create("Result", null, new Vector3(0f, -1.0f, 0f), 40, new Color(0.9f, 0.9f, 0.6f));
        }

        private TouchButton MakeButton(string name, string label, Vector3 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
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
