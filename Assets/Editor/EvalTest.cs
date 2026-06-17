using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardGame.EditorTools
{
    // Headless validation of the poker hand evaluator. Run via -executeMethod CardGame.EditorTools.EvalTest.RunTests
    public static class EvalTest
    {
        private static int _pass, _fail;

        public static void RunTests()
        {
            _pass = 0; _fail = 0;

            // High-hand categories
            Check("royal flush", HandEval.High(H("As Ks Qs Js Ts")).Category == 8);
            Check("royal flush high card", HandEval.High(H("As Ks Qs Js Ts")).K0 == 14);
            Check("wheel straight flush", Cat(HandEval.High(H("5s 4s 3s 2s As"))) == 8 && HandEval.High(H("5s 4s 3s 2s As")).K0 == 5);
            Check("quads beat full house", HandEval.High(H("9c 9d 9h 9s 2c")).CompareTo(HandEval.High(H("Kc Kd Kh 2s 2d"))) > 0);
            Check("flush beats straight", HandEval.High(H("Ah Kh 7h 4h 2h")).CompareTo(HandEval.High(H("9c 8d 7h 6s 5c"))) > 0);
            Check("ace-high straight", Cat(HandEval.High(H("Ah Kd Qc Js Td"))) == 4 && HandEval.High(H("Ah Kd Qc Js Td")).K0 == 14);
            Check("full house trip-rank tiebreak", HandEval.High(H("Kc Kd Kh 2s 2d")).CompareTo(HandEval.High(H("Qc Qd Qh Ac Ad"))) > 0);

            // Best 5 of 7
            Check("best 5 of 7 finds flush", Cat(HandEval.High(H("As Ks Qs Js 2s 3d 4c"))) == 5);

            // Eight-or-better low
            Check("8-low qualifies", Q(HandEval.Low8(H("8c 7d 6h 5s 4c"), out var q1)) && q1);
            Check("wheel is best low", HandEval.Low8(H("Ac 2d 3h 4s 5c"), out _).K0 == 5);
            Check("9-low does not qualify", !LowQ(H("9c 8d 7h 6s 5c")));
            Check("paired hand still makes low from 7", LowQ(H("8c 8d 7h 6s 5c 4d 3h")));

            // Razz (no qualifier)
            Check("razz best low ignores pairs", HandEval.RazzLow(H("Ac 2d 3h 4s 5c Kc Kd")).K0 == 5);
            Check("razz straight does not count", Cat(HandEval.RazzLow(H("Ac 2d 3h 4s 5c Kc Kd"))) == 0);

            // Low comparison: 7-5-4-3-2 is better (smaller) than 7-6-4-3-2
            Check("lower low wins", HandEval.RazzLow(H("7c 5d 4h 3s 2c")).CompareTo(HandEval.RazzLow(H("7c 6d 4h 3s 2c"))) < 0);

            // Omaha 2+3 constraint
            Check("omaha can make royal with 2 hole",
                Cat(HandEval.OmahaHigh(H("As Ks 2c 3d"), H("Qs Js Ts 4h 5h"))) == 8);
            Check("omaha cannot use board-only flush",
                Cat(HandEval.OmahaHigh(H("Ad 2c 3h 4s"), H("8s 9s Ts Js Qs"))) < 5);

            Debug.Log($"EVALTEST RESULT: {_pass}/{_pass + _fail} passed, {_fail} failed.");
            EditorApplication.Exit(_fail == 0 ? 0 : 1);
        }

        private static int Cat(HiValue v) => v.Category;
        private static bool Q(HiValue v) => true; // value used only to pass through out param
        private static bool LowQ(List<Card> cards) { HandEval.Low8(cards, out bool q); return q; }

        private static void Check(string name, bool ok)
        {
            if (ok) { _pass++; }
            else { _fail++; Debug.LogError($"EVALTEST FAIL: {name}"); }
        }

        // ---- parsing helpers ----
        private static List<Card> H(string s)
        {
            var list = new List<Card>();
            foreach (var tok in s.Split(' '))
                if (tok.Length == 2) list.Add(C(tok));
            return list;
        }

        private static Card C(string tok)
        {
            int rank = tok[0] switch
            {
                'A' => 14,
                'K' => 13,
                'Q' => 12,
                'J' => 11,
                'T' => 10,
                _ => tok[0] - '0'
            };
            Suit suit = tok[1] switch
            {
                'c' => Suit.Clubs,
                'd' => Suit.Diamonds,
                'h' => Suit.Hearts,
                _ => Suit.Spades
            };
            return new Card(rank, suit);
        }
    }
}
