using UnityEngine;

namespace HY3RIDOrigins.Combat
{
    // Shared runtime sprite generators for combat VFX. All sprites are 1×1 world unit
    // at the given radius (PPU = size). Swap with asset imports later without changing callers.
    internal static class SpriteUtil
    {
        internal static Sprite Circle(int radius, Color color)
        {
            int size = radius * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float r = radius - 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f, dy = y - radius + 0.5f;
                    pixels[y * size + x] = dx * dx + dy * dy <= r * r
                        ? (Color32)color : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        internal static Sprite Solid(int w, int h, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var c32 = (Color32)color;
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c32;
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, Mathf.Max(w, h));
        }
    }
}
