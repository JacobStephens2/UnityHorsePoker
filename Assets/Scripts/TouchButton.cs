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

        public void Init(string label, Color color, float width = 2.4f, float height = 1.1f)
        {
            _normal = color;

            _bg = gameObject.AddComponent<SpriteRenderer>();
            _bg.sprite = SpriteFactory.RoundedRect(240, 110, 28, color, color * 0.8f, 6);
            _bg.sortingOrder = 5;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, height);

            TextFactory.Create("Label", transform, new Vector3(0f, 0f, -0.1f), 50, Color.white);
            GetComponentInChildren<TextMesh>().text = label;
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
