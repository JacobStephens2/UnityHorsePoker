# HORSE Poker

A mobile (Android, portrait) **HORSE** mixed-poker game built in Unity 6 (6000.0.77f1) — play against three AI opponents through the five-game rotation:

- **H** — Texas Hold'em (high)
- **O** — Omaha Eight-or-Better (hi-lo split)
- **R** — Razz (ace-to-five low)
- **S** — Seven-Card Stud (high)
- **E** — Seven-Card Stud Eight-or-Better (hi-lo split)

The game rotates one variant per hand. Fixed-limit betting with blinds (flop games) or antes + bring-in (stud games), side pots, and hi-lo split pots with the 8-or-better qualifier. On your turn, tap **FOLD / CHECK-CALL / BET-RAISE**.

## Highlights

- **No art assets.** Cards, buttons, and table are drawn procedurally at runtime; all text uses Unity's built-in font.
- **Touch-friendly**, responsive layout that adapts to the screen aspect.
- **Validated headlessly before any UI** — see the tests below.

## Project layout

| Path | Purpose |
|------|---------|
| `Assets/Scripts/Card.cs`, `Deck.cs` | 52-card model (rank 2–14) |
| `Assets/Scripts/Poker.cs` | `Variant` enum + comparable `HiValue` |
| `Assets/Scripts/HandEval.cs` | One 5-card evaluator core: high, ace-to-five low, 8-or-better, Omaha 2+3 |
| `Assets/Scripts/Player.cs` | A seat (chips, hole/up cards, bets) |
| `Assets/Scripts/HorseTable.cs` | Engine: dealing, fixed-limit betting state machine, side pots, hi-lo split, rotation |
| `Assets/Scripts/Ai.cs` | Heuristic opponent (per-variant hand-strength → fold/call/raise) |
| `Assets/Scripts/PokerGame.cs` | Table UI + touch input + AI pacing |
| `Assets/Scripts/GameBootstrap.cs` | Camera + game setup |
| `Assets/Editor/CIBuild.cs` | Headless scene setup + Android APK build |
| `Assets/Editor/EvalTest.cs` | Hand-evaluator unit checks |
| `Assets/Editor/SimTest.cs` | All-AI soak test (chip conservation) |

## Headless tests

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.0.77f1/Unity.app/Contents/MacOS/Unity
# 17/17 hand-evaluation assertions
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod CardGame.EditorTools.EvalTest.RunTests -logFile eval.log
# 400 all-AI hands: rotation, termination, chip conservation
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod CardGame.EditorTools.SimTest.RunSim -logFile sim.log
```

## Building an APK (headless)

```sh
"$UNITY" -batchmode -quit -nographics -buildTarget Android \
  -projectPath . \
  -executeMethod CardGame.EditorTools.CIBuild.BuildAndroid \
  -logFile build.log
```

The signed (debug-keystore) APK is written to `Builds/CardGame.apk` — installable via `adb install`.
