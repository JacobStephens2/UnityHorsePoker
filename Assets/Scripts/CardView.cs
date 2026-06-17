using UnityEngine;

namespace CardGame
{
    // Visual representation of a single card: a white rounded sprite with rank+suit labels.
    public class CardView : MonoBehaviour
    {
        private SpriteRenderer _bg;
        private TextMesh _topLabel;
        private TextMesh _centerLabel;

        public void Init()
        {
            _bg = gameObject.AddComponent<SpriteRenderer>();
            _bg.sprite = SpriteFactory.RoundedRect(200, 280, 24, Color.white, new Color(0.85f, 0.85f, 0.88f));
            _bg.sortingOrder = 0;

            _topLabel = TextFactory.Create("TopLabel", transform, new Vector3(-0.82f, 1.22f, 0f), 44, Color.black, TextAnchor.UpperLeft);
            _centerLabel = TextFactory.Create("CenterLabel", transform, new Vector3(0f, -0.15f, 0f), 95, Color.black);
        }

        public void Show(Card card)
        {
            string rank = card.RankLabel;
            string suit = card.SuitLabel;
            _topLabel.text = rank + "\n" + suit;
            _topLabel.color = card.Color;
            _centerLabel.text = suit;
            _centerLabel.color = card.Color;
        }
    }
}
