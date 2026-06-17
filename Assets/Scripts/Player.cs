using System.Collections.Generic;

namespace CardGame
{
    // A seat at the table.
    public class Player
    {
        public string Name;
        public bool IsHuman;
        public int Chips;

        public readonly List<Card> Down = new List<Card>(); // hidden hole cards
        public readonly List<Card> Up = new List<Card>();    // exposed cards (stud games)

        public bool Folded;
        public bool AllIn;
        public int StreetBet;     // chips committed on the current street
        public int Contributed;   // total chips committed this hand (for side pots)

        public int LastResultDelta; // chips won/lost in the just-finished hand (for UI)

        public Player(string name, int chips, bool human)
        {
            Name = name;
            Chips = chips;
            IsHuman = human;
        }

        public bool InHand => !Folded;
        public bool CanAct => !Folded && !AllIn;

        public List<Card> AllCards()
        {
            var list = new List<Card>(Down.Count + Up.Count);
            list.AddRange(Down);
            list.AddRange(Up);
            return list;
        }

        public void ResetForHand()
        {
            Down.Clear();
            Up.Clear();
            Folded = false;
            AllIn = false;
            StreetBet = 0;
            Contributed = 0;
            LastResultDelta = 0;
        }

        // Move up to `amount` chips into the pot; returns the amount actually committed.
        public int Commit(int amount)
        {
            amount = System.Math.Min(amount, Chips);
            Chips -= amount;
            StreetBet += amount;
            Contributed += amount;
            if (Chips == 0) AllIn = true;
            return amount;
        }
    }
}
