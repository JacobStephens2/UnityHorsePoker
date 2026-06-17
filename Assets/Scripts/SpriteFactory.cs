using UnityEngine;

namespace CardGame
{
    // Generates simple rounded-rectangle sprites at runtime so the game needs no art assets.
    public static class SpriteFactory
    {
        public static Sprite RoundedRect(int w, int h, int radius, Color fill, Color border, int borderPx = 4)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;
                    if (!InsideRounded(x, y, w, h, radius))
                        c = Color.clear;
                    else if (InsideRounded(x, y, w, h, radius, borderPx) == false)
                        c = border;
                    else
                        c = fill;
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // True if (x,y) lies inside a rounded rect inset by `inset` pixels on all sides.
        private static bool InsideRounded(int x, int y, int w, int h, int radius, int inset = 0)
        {
            int minX = inset, minY = inset, maxX = w - 1 - inset, maxY = h - 1 - inset;
            if (x < minX || x > maxX || y < minY || y > maxY) return false;

            int r = Mathf.Max(0, radius - inset);
            // Corner centers
            int cxL = minX + r, cxR = maxX - r, cyB = minY + r, cyT = maxY - r;

            int dx = 0, dy = 0;
            if (x < cxL && y < cyB) { dx = x - cxL; dy = y - cyB; }
            else if (x > cxR && y < cyB) { dx = x - cxR; dy = y - cyB; }
            else if (x < cxL && y > cyT) { dx = x - cxL; dy = y - cyT; }
            else if (x > cxR && y > cyT) { dx = x - cxR; dy = y - cyT; }
            else return true; // straight edges / interior

            return dx * dx + dy * dy <= r * r;
        }
    }
}
