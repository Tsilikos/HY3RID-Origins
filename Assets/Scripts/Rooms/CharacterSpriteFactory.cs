using UnityEngine;
using HY3RIDOrigins.Data;

namespace HY3RIDOrigins.Rooms
{
    // Generates top-down character sprites at runtime.
    // The portrait PNGs (1084×1451) are for UI only (dialogue, evolution cards, HUD).
    // Top-down view uses colored circles with a forward-direction indicator.
    // Replace with proper sprite sheets when available.
    public static class CharacterSpriteFactory
    {
        private const int TextureSize = 128; // pixels
        private const float PixelsPerUnit = 128f;

        public static Sprite CreateTopDownSprite(Stats stats)
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[TextureSize * TextureSize];
            float cx = TextureSize / 2f;
            float cy = TextureSize / 2f;
            float r  = TextureSize * 0.42f; // body radius in pixels

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= r)
                    {
                        // Rim darkening
                        float t = dist / r;
                        Color body = Color.Lerp(stats.TeamColor, stats.TeamColor * 0.6f, t * t);
                        body.a = 1f;
                        pixels[y * TextureSize + x] = body;
                    }
                    else
                    {
                        pixels[y * TextureSize + x] = Color.clear;
                    }
                }
            }

            // Direction indicator: small filled triangle pointing UP (toward +Y = "forward").
            // When the sprite is rotated in the scene, this points toward the aim direction.
            DrawDirectionTriangle(pixels, stats.TeamColor * 0.5f, cx, cy, r);

            // Highlight dot (gives 3D appearance)
            DrawHighlight(pixels, stats.TeamColor, cx, cy, r);

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize),
                                  Vector2.one * 0.5f, PixelsPerUnit);
        }

        private static void DrawDirectionTriangle(Color32[] pixels, Color color,
            float cx, float cy, float r)
        {
            // Triangle tip at (cx, cy + r * 0.85), base at y = cy + r * 0.55
            float tipY  = cy + r * 0.85f;
            float baseY = cy + r * 0.55f;
            float halfW = r * 0.22f;

            for (int y = (int)baseY; y <= (int)tipY; y++)
            {
                float progress = (y - baseY) / (tipY - baseY); // 0 at base, 1 at tip
                float hw = halfW * (1f - progress);
                for (int x = (int)(cx - hw); x <= (int)(cx + hw); x++)
                {
                    if (x < 0 || x >= TextureSize || y < 0 || y >= TextureSize) continue;
                    pixels[y * TextureSize + x] = color;
                }
            }
        }

        private static void DrawHighlight(Color32[] pixels, Color teamColor,
            float cx, float cy, float r)
        {
            float hx = cx - r * 0.28f;
            float hy = cy + r * 0.28f;
            float hr = r * 0.18f;
            Color highlight = Color.Lerp(teamColor, Color.white, 0.7f);
            highlight.a = 0.55f;

            for (int y = (int)(hy - hr); y <= (int)(hy + hr); y++)
            {
                for (int x = (int)(hx - hr); x <= (int)(hx + hr); x++)
                {
                    if (x < 0 || x >= TextureSize || y < 0 || y >= TextureSize) continue;
                    float dx = x - hx, dy = y - hy;
                    if (dx * dx + dy * dy <= hr * hr)
                    {
                        int idx = y * TextureSize + x;
                        Color existing = pixels[idx];
                        pixels[idx] = Color.Lerp(existing, highlight, highlight.a);
                    }
                }
            }
        }

        // Creates a simple circle sprite for enemies given a color.
        public static Sprite CreateEnemySprite(Color color, int size = 96)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float cx = size / 2f, cy = size / 2f, r = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r)
                    {
                        Color c = Color.Lerp(color, color * 0.5f, dist / r);
                        c.a = 1f;
                        pixels[y * size + x] = c;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, (float)size);
        }
    }
}
