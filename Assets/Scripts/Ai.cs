using System.Collections.Generic;

namespace CardGame
{
    // Heuristic opponent: estimates hand strength for the current variant, then folds/calls/raises.
    public static class Ai
    {
        private static readonly System.Random Rng = new System.Random();

        public static ActionType Decide(HorseTable t, Player p, ActionView v, int currentBet, int betSize)
        {
            float s = Strength(t, p);
            double r = Rng.NextDouble();

            if (v.CanCheck)
            {
                if (s > 0.55f && v.CanRaise && r < 0.55) return ActionType.Raise;
                return ActionType.Check;
            }

            // Facing a bet.
            if (s < 0.30f) return r < 0.12 ? ActionType.Call : ActionType.Fold;
            if (s < 0.62f) return (v.CanRaise && r < 0.15) ? ActionType.Raise : ActionType.Call;
            return (v.CanRaise && r < 0.70) ? ActionType.Raise : ActionType.Call;
        }

        private static float Strength(HorseTable t, Player p)
        {
            var v = t.Variant;
            if (VariantInfo.IsCommunity(v))
            {
                var known = new List<Card>(p.Down); known.AddRange(t.Board);
                if (v == Variant.Holdem)
                    return known.Count >= 5 ? HighStrength(HandEval.High(known)) : PreflopHigh(p.Down);
                // Omaha hi-lo
                if (t.Board.Count >= 3)
                {
                    float hi = HighStrength(HandEval.OmahaHigh(p.Down, t.Board));
                    float lo = LowPartial(known);
                    return System.Math.Max(hi, lo * 0.9f);
                }
                return PreflopHigh(p.Down) * 0.8f + LowPartial(p.Down) * 0.2f;
            }

            // Stud family
            var all = p.AllCards();
            if (v == Variant.Razz)
                return all.Count >= 5 ? LowStrength(HandEval.RazzLow(all)) : LowPartial(all);
            if (v == Variant.Stud)
                return all.Count >= 5 ? HighStrength(HandEval.High(all)) : PartialHigh(all);
            // Stud hi-lo
            float h = all.Count >= 5 ? HighStrength(HandEval.High(all)) : PartialHigh(all);
            float l = LowPartial(all);
            return System.Math.Max(h, l * 0.9f);
        }

        private static float HighStrength(HiValue v)
        {
            switch (v.Category)
            {
                case 0: return 0.08f + v.K0 / 14f * 0.12f;
                case 1: return 0.30f + v.K0 / 14f * 0.10f;
                case 2: return 0.52f;
                case 3: return 0.68f;
                case 4: return 0.78f;
                case 5: return 0.85f;
                case 6: return 0.92f;
                case 7: return 0.97f;
                default: return 1.0f;
            }
        }

        private static float LowStrength(HiValue lo)
        {
            if (!lo.HasValue) return 0.1f;
            if (lo.Category > 0) return 0.20f;          // a pair ruins a low
            return Clamp01(1f - (lo.K0 - 5) * 0.08f);   // 5-high best, falls off as top card rises
        }

        private static float PreflopHigh(List<Card> hole)
        {
            int hi = 2, lo = 14; bool pair = false;
            var seen = new HashSet<int>();
            foreach (var c in hole)
            {
                if (c.Rank > hi) hi = c.Rank;
                if (c.Rank < lo) lo = c.Rank;
                if (!seen.Add(c.Rank)) pair = true;
            }
            if (pair) return 0.45f + hi / 14f * 0.20f;
            return 0.18f + hi / 14f * 0.22f;
        }

        private static float PartialHigh(List<Card> cards)
        {
            var seen = new HashSet<int>(); bool pair = false; int hi = 2;
            foreach (var c in cards) { if (c.Rank > hi) hi = c.Rank; if (!seen.Add(c.Rank)) pair = true; }
            return (pair ? 0.45f : 0.18f) + hi / 14f * 0.18f;
        }

        private static float LowPartial(List<Card> cards)
        {
            var low = new HashSet<int>();
            foreach (var c in cards) { int r = c.Rank == 14 ? 1 : c.Rank; if (r <= 8) low.Add(r); }
            int d = System.Math.Min(low.Count, 5);
            return Clamp01(d / 5f * 0.8f);
        }

        private static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
    }
}
