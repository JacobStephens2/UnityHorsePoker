using UnityEngine;

namespace CardGame
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }

    // Immutable playing card. Rank 2..14 with Ace = 14 (treated as 1 in low evaluation).
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
            14 => "A",
            13 => "K",
            12 => "Q",
            11 => "J",
            10 => "T",
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

        public override string ToString() => RankLabel + SuitLabel;
    }
}
