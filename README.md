# Card Higher Lower

A small 2D **"Higher or Lower"** card game built in Unity 6 (6000.0.77f1), targeting Android.

Draw a card, then tap **HIGHER** or **LOWER** to guess whether the next card outranks it. Correct guesses build a streak; a wrong guess resets it. Score and best streak are shown up top.

## Highlights

- **No art assets.** Cards, buttons, and table are drawn procedurally at runtime (`SpriteFactory`), and all text uses Unity's built-in font (`TextFactory`).
- **Touch-friendly.** Buttons use `Physics2D` hit-testing, so the same input works for mouse and Android touch.
- **Scene built from code.** A single `GameBootstrap` component sets up the orthographic camera and game; the rest is constructed at play time.

## Project layout

| Path | Purpose |
|------|---------|
| `Assets/Scripts/Card.cs` | Card value type (rank, suit, color, labels) |
| `Assets/Scripts/Deck.cs` | 52-card deck, Fisher–Yates shuffle, draw |
| `Assets/Scripts/SpriteFactory.cs` | Runtime rounded-rectangle sprite generation |
| `Assets/Scripts/TextFactory.cs` | Built-in-font world-space labels |
| `Assets/Scripts/CardView.cs` | Renders a single card |
| `Assets/Scripts/TouchButton.cs` | Tappable HIGHER/LOWER buttons |
| `Assets/Scripts/GameController.cs` | Game loop, input, scoring |
| `Assets/Scripts/GameBootstrap.cs` | Camera + game setup |
| `Assets/Editor/CIBuild.cs` | Headless scene setup + Android APK build |

## Building an APK (headless)

```sh
UNITY=/Applications/Unity/Hub/Editor/6000.0.77f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -quit -nographics -buildTarget Android \
  -projectPath . \
  -executeMethod CardGame.EditorTools.CIBuild.BuildAndroid \
  -logFile build.log
```

The signed (debug-keystore) APK is written to `Builds/CardGame.apk` — installable via `adb install`.
