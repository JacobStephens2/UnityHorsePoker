using System;
using UnityEngine;

namespace CardGame
{
    // A tappable button drawn from a procedural sprite with a text label and a 2D collider.
    public class TouchButton : MonoBehaviour
    {
        public event Action Clicked;

        private SpriteRenderer _bg;
        private Color _normal;
        private TextMesh _label;

        public void Init(string label, Color color, float width = 2.4f, float height = 1.1f)
        {
            _normal = color;

            _bg = gameObject.AddComponent<SpriteRenderer>();
            _bg.sprite = SpriteFactory.RoundedRect(240, 110, 28, color, color * 0.8f, 6);
            _bg.sortingOrder = 5;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);

            _label = TextFactory.Create("Label", transform, new Vector3(0f, 0f, -0.1f), 50, Color.white);
            _label.text = label;
        }

        public void SetLabel(string s) { if (_label != null) _label.text = s; }

        public void SetColor(Color color)
        {
            _normal = color;
            if (_bg != null) _bg.sprite = SpriteFactory.RoundedRect(240, 110, 28, color, color * 0.8f, 6);
        }

        public void Press()
        {
            Clicked?.Invoke();
        }

        public void Flash()
        {
            // Brief visual feedback handled by GameController via coroutine-free lerp.
            _bg.color = Color.white;
        }

        public void ResetTint()
        {
            _bg.color = _normal;
        }
    }
}
