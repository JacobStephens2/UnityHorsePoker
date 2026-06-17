using System.Collections.Generic;

namespace CardGame
{
    // A standard 52-card deck (ranks 2..14) with Fisher-Yates shuffle.
    public class Deck
    {
        private readonly List<Card> _cards = new List<Card>(52);
        private readonly System.Random _rng;
        private int _index;

        public Deck(int seed = 0)
        {
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            Reset();
        }

        public int Remaining => _cards.Count - _index;

        public void Reset()
        {
            _cards.Clear();
            for (int s = 0; s < 4; s++)
                for (int r = 2; r <= 14; r++)
                    _cards.Add(new Card(r, (Suit)s));
            Shuffle();
        }

        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
            _index = 0;
        }

        public Card Deal() => _cards[_index++];
    }
}
