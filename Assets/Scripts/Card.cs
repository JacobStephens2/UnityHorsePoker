using UnityEngine;

namespace CardGame
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }

    // Immutable playing-card value object. Rank 1=Ace .. 13=King.
    public readonly struct Card
    {
        public readonly int Rank;
        public readonly Suit Suit;

        public Card(int rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public bool IsRed => Suit == Suit.Hearts || Suit == Suit.Diamonds;

        public string RankLabel => Rank switch
        {
            1 => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => Rank.ToString()
        };

        // Single-letter suit so it renders with the built-in font on any platform.
        public string SuitLabel => Suit switch
        {
            Suit.Clubs => "C",
            Suit.Diamonds => "D",
            Suit.Hearts => "H",
            _ => "S"
        };

        public Color Color => IsRed ? new Color(0.82f, 0.13f, 0.18f) : new Color(0.10f, 0.10f, 0.12f);
    }
}
