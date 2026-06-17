using UnityEditor;
using UnityEngine;

namespace CardGame.EditorTools
{
    // Headless engine soak test: play many all-AI HORSE hands, assert termination + chip conservation.
    public static class SimTest
    {
        public static void RunSim()
        {
            int fails = 0;
            try
            {
                var t = new HorseTable(4, 7777);
                foreach (var p in t.Players) p.IsHuman = false; // fully automated
                int expectedTotal = 0; foreach (var p in t.Players) expectedTotal += p.Chips;

                int hands = 400;
                var counts = new int[5];
                for (int h = 0; h < hands; h++)
                {
                    t.StartHand();
                    counts[(int)t.Variant]++;
                    int guard = 0;
                    while (t.State != TableState.HandOver)
                    {
                        t.StepOnce();
                        if (t.AwaitingHuman) { Debug.LogError("SIMTEST FAIL: engine waited for human in all-AI sim"); fails++; t.HumanAct(ActionType.Fold); }
                        if (++guard > 200000) { Debug.LogError("SIMTEST FAIL: hand did not terminate"); fails++; break; }
                    }
                    int total = 0; bool neg = false;
                    foreach (var p in t.Players) { total += p.Chips; if (p.Chips < 0) neg = true; }
                    if (neg) { Debug.LogError($"SIMTEST FAIL: negative chips at hand {h}"); fails++; }
                    if (total != expectedTotal) { Debug.LogError($"SIMTEST FAIL: chips not conserved at hand {h}: {total} != {expectedTotal}"); fails++; }
                    if (fails > 5) break;
                }
                Debug.Log($"SIMTEST RESULT: {hands} hands, fails={fails}. Variant deal counts H={counts[0]} O={counts[1]} R={counts[2]} S={counts[3]} E={counts[4]}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("SIMTEST EXCEPTION: " + e);
                fails++;
            }
            EditorApplication.Exit(fails == 0 ? 0 : 1);
        }
    }
}
