using UnityEngine;

namespace CardGame
{
    // Creates world-space TextMesh labels using Unity's built-in font (no font asset needed).
    public static class TextFactory
    {
        private static Font _font;

        public static Font BuiltinFont
        {
            get
            {
                if (_font == null)
                {
                    // Unity 2022+ renamed the legacy built-in font.
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                            ?? Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                return _font;
            }
        }

        public static TextMesh Create(string name, Transform parent, Vector3 localPos, int fontSize,
                                      Color color, TextAnchor anchor = TextAnchor.MiddleCenter, float charSize = 0.1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var tm = go.AddComponent<TextMesh>();
            tm.font = BuiltinFont;
            tm.fontSize = fontSize;
            tm.characterSize = charSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = BuiltinFont.material;
            mr.sortingOrder = 10;
            return tm;
        }
    }
}
