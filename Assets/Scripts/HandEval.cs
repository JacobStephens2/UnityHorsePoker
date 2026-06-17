using System.Collections.Generic;

namespace CardGame
{
    // Poker hand evaluation. A single 5-card core, parameterized for HIGH and ace-to-five LOW.
    public static class HandEval
    {
        // Evaluate exactly 5 cards.
        //   aceLow  : treat Ace as 1 (for lowball)
        //   ignoreSF: ignore straights & flushes (ace-to-five lowball does not count them)
        public static HiValue Eval5(Card a, Card b, Card c, Card d, Card e, bool aceLow, bool ignoreSF)
        {
            int[] r = { Rk(a, aceLow), Rk(b, aceLow), Rk(c, aceLow), Rk(d, aceLow), Rk(e, aceLow) };
            bool flush = !ignoreSF && a.Suit == b.Suit && b.Suit == c.Suit && c.Suit == d.Suit && d.Suit == e.Suit;

            // sort ranks descending
            for (int i = 0; i < 5; i++)
                for (int j = i + 1; j < 5; j++)
                    if (r[j] > r[i]) { int t = r[i]; r[i] = r[j]; r[j] = t; }

            // group by rank
            int[] gr = new int[5], gc = new int[5]; int gn = 0;
            for (int i = 0; i < 5; i++)
            {
                int found = -1;
                for (int k = 0; k < gn; k++) if (gr[k] == r[i]) { found = k; break; }
                if (found < 0) { gr[gn] = r[i]; gc[gn] = 1; gn++; }
                else gc[found]++;
            }
            // order groups by (count desc, rank desc)
            for (int i = 0; i < gn; i++)
                for (int j = i + 1; j < gn; j++)
                    if (gc[j] > gc[i] || (gc[j] == gc[i] && gr[j] > gr[i]))
                    {
                        int t = gc[i]; gc[i] = gc[j]; gc[j] = t;
                        t = gr[i]; gr[i] = gr[j]; gr[j] = t;
                    }

            bool straight = false; int straightHigh = 0;
            if (!ignoreSF && gn == 5)
            {
                if (r[0] - r[4] == 4) { straight = true; straightHigh = r[0]; }
                else if (r[0] == 14 && r[1] == 5 && r[2] == 4 && r[3] == 3 && r[4] == 2) { straight = true; straightHigh = 5; } // wheel
            }

            int cat;
            if (straight && flush) cat = 8;
            else if (gc[0] == 4) cat = 7;
            else if (gc[0] == 3 && gn >= 2 && gc[1] == 2) cat = 6;
            else if (flush) cat = 5;
            else if (straight) cat = 4;
            else if (gc[0] == 3) cat = 3;
            else if (gc[0] == 2 && gn >= 2 && gc[1] == 2) cat = 2;
            else if (gc[0] == 2) cat = 1;
            else cat = 0;

            HiValue v = default;
            v.HasValue = true;
            v.Category = cat;
            if (cat == 4 || cat == 8)
            {
                v.K0 = straightHigh;
            }
            else
            {
                v.K0 = gn > 0 ? gr[0] : 0;
                v.K1 = gn > 1 ? gr[1] : 0;
                v.K2 = gn > 2 ? gr[2] : 0;
                v.K3 = gn > 3 ? gr[3] : 0;
                v.K4 = gn > 4 ? gr[4] : 0;
            }
            return v;
        }

        private static int Rk(Card c, bool aceLow) => (aceLow && c.Rank == 14) ? 1 : c.Rank;

        // Best 5-of-N for a free-choice game (Hold'em/Stud high, Razz/low). wantMax=false picks the best LOW.
        public static HiValue BestOf(IReadOnlyList<Card> cards, bool aceLow, bool ignoreSF, bool wantMax)
        {
            int n = cards.Count;
            HiValue best = default; bool has = false;
            for (int a = 0; a < n - 4; a++)
                for (int b = a + 1; b < n - 3; b++)
                    for (int c = b + 1; c < n - 2; c++)
                        for (int d = c + 1; d < n - 1; d++)
                            for (int e = d + 1; e < n; e++)
                            {
                                var v = Eval5(cards[a], cards[b], cards[c], cards[d], cards[e], aceLow, ignoreSF);
                                if (!has) { best = v; has = true; }
                                else { int cmp = v.CompareTo(best); if (wantMax ? cmp > 0 : cmp < 0) best = v; }
                            }
            return best;
        }

        // Omaha: must use exactly 2 hole cards + 3 board cards.
        public static HiValue BestOmaha(IReadOnlyList<Card> hole, IReadOnlyList<Card> board, bool aceLow, bool ignoreSF, bool wantMax)
        {
            HiValue best = default; bool has = false;
            int nh = hole.Count, nb = board.Count;
            for (int h1 = 0; h1 < nh - 1; h1++)
                for (int h2 = h1 + 1; h2 < nh; h2++)
                    for (int b1 = 0; b1 < nb - 2; b1++)
                        for (int b2 = b1 + 1; b2 < nb - 1; b2++)
                            for (int b3 = b2 + 1; b3 < nb; b3++)
                            {
                                var v = Eval5(hole[h1], hole[h2], board[b1], board[b2], board[b3], aceLow, ignoreSF);
                                if (!has) { best = v; has = true; }
                                else { int cmp = v.CompareTo(best); if (wantMax ? cmp > 0 : cmp < 0) best = v; }
                            }
            return best;
        }

        // A LOW value qualifies for "eight or better" when it is five distinct ranks all <= 8.
        public static bool LowQualifies(HiValue low) => low.HasValue && low.Category == 0 && low.K0 <= 8;

        // --- Convenience evaluators per game type (cards = all of a player's available cards). ---

        public static HiValue High(IReadOnlyList<Card> cards) => BestOf(cards, false, false, true);

        public static HiValue RazzLow(IReadOnlyList<Card> cards) => BestOf(cards, true, true, false);

        public static HiValue Low8(IReadOnlyList<Card> cards, out bool qualifies)
        {
            var lo = BestOf(cards, true, true, false);
            qualifies = LowQualifies(lo);
            return lo;
        }

        public static HiValue OmahaHigh(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
            => BestOmaha(hole, board, false, false, true);

        public static HiValue OmahaLow8(IReadOnlyList<Card> hole, IReadOnlyList<Card> board, out bool qualifies)
        {
            var lo = BestOmaha(hole, board, true, true, false);
            qualifies = LowQualifies(lo);
            return lo;
        }
    }
}
