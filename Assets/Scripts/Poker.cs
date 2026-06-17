using System;

namespace CardGame
{
    // The five games of HORSE, rotated each hand.
    public enum Variant
    {
        Holdem,     // H - Texas Hold'em (high)
        OmahaHiLo,  // O - Omaha Eight-or-Better (hi-lo split)
        Razz,       // R - Razz (ace-to-five low only)
        Stud,       // S - Seven-card Stud (high)
        StudHiLo    // E - Seven-card Stud Eight-or-Better (hi-lo split)
    }

    public static class VariantInfo
    {
        public static string Title(Variant v) => v switch
        {
            Variant.Holdem => "TEXAS HOLD'EM",
            Variant.OmahaHiLo => "OMAHA HI-LO",
            Variant.Razz => "RAZZ",
            Variant.Stud => "SEVEN-CARD STUD",
            _ => "STUD HI-LO"
        };

        public static char Letter(Variant v) => v switch
        {
            Variant.Holdem => 'H',
            Variant.OmahaHiLo => 'O',
            Variant.Razz => 'R',
            Variant.Stud => 'S',
            _ => 'E'
        };

        public static bool IsCommunity(Variant v) => v == Variant.Holdem || v == Variant.OmahaHiLo;
        public static bool IsStud(Variant v) => v == Variant.Razz || v == Variant.Stud || v == Variant.StudHiLo;
        public static bool HasLow(Variant v) => v == Variant.OmahaHiLo || v == Variant.StudHiLo;
        public static bool LowOnly(Variant v) => v == Variant.Razz;
    }

    // Comparable poker hand strength. Higher Category/kickers = stronger HIGH hand.
    // For low evaluation the SMALLEST HiValue is the best low.
    public struct HiValue : IComparable<HiValue>
    {
        public bool HasValue;
        public int Category;            // 0 high,1 pair,2 two pair,3 trips,4 straight,5 flush,6 full,7 quads,8 straight flush
        public int K0, K1, K2, K3, K4;  // tiebreak ranks, grouped high->low

        public int CompareTo(HiValue o)
        {
            if (Category != o.Category) return Category.CompareTo(o.Category);
            if (K0 != o.K0) return K0.CompareTo(o.K0);
            if (K1 != o.K1) return K1.CompareTo(o.K1);
            if (K2 != o.K2) return K2.CompareTo(o.K2);
            if (K3 != o.K3) return K3.CompareTo(o.K3);
            return K4.CompareTo(o.K4);
        }

        public static readonly string[] CategoryNames =
        {
            "High Card", "Pair", "Two Pair", "Three of a Kind", "Straight",
            "Flush", "Full House", "Four of a Kind", "Straight Flush"
        };

        public string Name => HasValue && Category >= 0 && Category < CategoryNames.Length ? CategoryNames[Category] : "-";
    }
}
