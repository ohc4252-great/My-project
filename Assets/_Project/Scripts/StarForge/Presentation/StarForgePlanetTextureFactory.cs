using System.Collections.Generic;
using UnityEngine;

namespace StarForge.Presentation
{
    public enum StarForgePlanetTheme
    {
        Rock,
        Ice,
        Terran,
        Lava,
        Ocean,
        Life,
        Gas,
        Star,
        BlackHole
    }

    public sealed class StarForgePlanetSurface
    {
        public StarForgePlanetTheme theme;
        public Texture2D baseMap;
        public Texture2D emissionMap;
    }

    public static class StarForgePlanetTextureFactory
    {
        private const int Width = 256;
        private const int Height = 128;

        private static readonly Dictionary<int, StarForgePlanetSurface> cache =
            new Dictionary<int, StarForgePlanetSurface>();

        public static StarForgePlanetTheme GetTheme(int level)
        {
            if (level <= 7)
            {
                return StarForgePlanetTheme.Rock;
            }

            switch (level)
            {
                case 8: return StarForgePlanetTheme.Ice;
                case 9: return StarForgePlanetTheme.Terran;
                case 10: return StarForgePlanetTheme.Lava;
                case 11: return StarForgePlanetTheme.Ocean;
                case 12: return StarForgePlanetTheme.Life;
            }

            if (level <= 15)
            {
                return StarForgePlanetTheme.Gas;
            }

            if (level <= 27)
            {
                return StarForgePlanetTheme.Star;
            }

            return StarForgePlanetTheme.BlackHole;
        }

        public static StarForgePlanetSurface Get(int level, Color baseColor)
        {
            StarForgePlanetSurface surface;
            if (cache.TryGetValue(level, out surface) && surface != null && surface.baseMap != null)
            {
                return surface;
            }

            surface = Generate(level, baseColor);
            cache[level] = surface;
            return surface;
        }

        private static StarForgePlanetSurface Generate(int level, Color baseColor)
        {
            StarForgePlanetTheme theme = GetTheme(level);
            float seed = 11.31f * (level + 7);

            bool emissive = theme == StarForgePlanetTheme.Lava
                || theme == StarForgePlanetTheme.Star
                || theme == StarForgePlanetTheme.BlackHole;

            Color32[] basePixels = new Color32[Width * Height];
            Color32[] emissionPixels = emissive ? new Color32[Width * Height] : null;

            for (int y = 0; y < Height; y++)
            {
                float v = (float)y / (Height - 1);
                for (int x = 0; x < Width; x++)
                {
                    float u = (float)x / (Width - 1);
                    Color baseCol;
                    Color emissionCol;
                    Shade(theme, level, u, v, baseColor, seed, out baseCol, out emissionCol);

                    int index = y * Width + x;
                    basePixels[index] = ToColor32(baseCol);
                    if (emissionPixels != null)
                    {
                        emissionPixels[index] = ToColor32(emissionCol);
                    }
                }
            }

            StarForgePlanetSurface surface = new StarForgePlanetSurface();
            surface.theme = theme;
            surface.baseMap = BuildTexture("StarForge Surface " + level, basePixels);
            surface.emissionMap = emissionPixels != null
                ? BuildTexture("StarForge Emission " + level, emissionPixels)
                : null;
            return surface;
        }

        private static void Shade(
            StarForgePlanetTheme theme,
            int level,
            float u,
            float v,
            Color baseColor,
            float seed,
            out Color baseCol,
            out Color emissionCol)
        {
            emissionCol = Color.black;

            switch (theme)
            {
                case StarForgePlanetTheme.Rock:
                {
                    float n = Fbm(u, v, 3.2f, 4, seed);
                    float detail = Fbm(u, v, 7f, 2, seed + 50f);
                    baseCol = baseColor * (0.5f + 0.55f * n);
                    if (detail > 0.62f)
                    {
                        baseCol *= 0.8f;
                    }

                    break;
                }
                case StarForgePlanetTheme.Ice:
                {
                    float n = Fbm(u, v, 4f, 4, seed);
                    baseCol = Color.Lerp(baseColor, Color.white, 0.35f + 0.4f * n);
                    float crack = Fbm(u, v, 9f, 2, seed + 9f);
                    if (crack > 0.68f)
                    {
                        baseCol *= 0.82f;
                    }

                    break;
                }
                case StarForgePlanetTheme.Terran:
                {
                    float n = Fbm(u, v, 3f, 4, seed);
                    if (n > 0.52f)
                    {
                        float detail = Fbm(u, v, 8f, 2, seed + 3f);
                        baseCol = baseColor * (0.8f + 0.4f * detail);
                    }
                    else
                    {
                        Color sea = Color.Lerp(baseColor, new Color(0.16f, 0.32f, 0.55f), 0.65f);
                        baseCol = sea * (0.8f + 0.3f * n);
                    }

                    break;
                }
                case StarForgePlanetTheme.Lava:
                {
                    float r = Fbm(u, v, 3.6f, 4, seed);
                    float ridge = 1f - Mathf.Abs(2f * r - 1f);
                    float crack = Mathf.InverseLerp(0.8f, 0.97f, ridge);
                    float pool = Fbm(u, v, 2.4f, 3, seed + 8f);
                    float poolT = Mathf.InverseLerp(0.72f, 0.9f, pool);
                    float heat = Mathf.Max(crack, poolT * 0.95f);

                    Color rock = baseColor * 0.25f * (0.7f + 0.5f * Fbm(u, v, 7f, 2, seed + 4f));
                    Color hot = new Color(1f, 0.5f, 0.12f);
                    baseCol = Color.Lerp(rock, hot * 0.9f, heat * 0.85f);
                    emissionCol = hot * heat;
                    break;
                }
                case StarForgePlanetTheme.Ocean:
                {
                    float n = Fbm(u, v, 4.5f, 3, seed);
                    baseCol = baseColor * (0.62f + 0.5f * n);
                    float cloud = Fbm(u + 0.15f * n, v, 5f, 3, seed + 21f);
                    if (cloud > 0.64f)
                    {
                        baseCol = Color.Lerp(baseCol, Color.white, Mathf.InverseLerp(0.64f, 0.85f, cloud) * 0.75f);
                    }

                    break;
                }
                case StarForgePlanetTheme.Life:
                {
                    float n = Fbm(u, v, 3.2f, 4, seed);
                    if (n > 0.54f)
                    {
                        float detail = Fbm(u, v, 8f, 2, seed + 5f);
                        baseCol = baseColor * (0.75f + 0.4f * detail);
                    }
                    else
                    {
                        baseCol = new Color(0.1f, 0.3f, 0.62f) * (0.75f + 0.4f * n);
                    }

                    float polar = Mathf.Abs(v - 0.5f) * 2f;
                    if (polar > 0.82f)
                    {
                        baseCol = Color.Lerp(baseCol, Color.white, Mathf.InverseLerp(0.82f, 0.95f, polar));
                    }

                    float cloud = Fbm(u + 0.1f * n, v, 5.5f, 3, seed + 33f);
                    if (cloud > 0.68f)
                    {
                        baseCol = Color.Lerp(baseCol, Color.white, Mathf.InverseLerp(0.68f, 0.88f, cloud) * 0.65f);
                    }

                    break;
                }
                case StarForgePlanetTheme.Gas:
                {
                    float turbulence = Fbm(u, v, 4f, 4, seed) - 0.5f;
                    float bandCount = 7f + (level - 13) * 2f;
                    float band = Mathf.Sin((v + turbulence * 0.18f) * Mathf.PI * bandCount) * 0.5f + 0.5f;

                    Color light = ClampColor(baseColor * 1.25f + new Color(0.08f, 0.08f, 0.08f));
                    Color dark = baseColor * 0.62f;
                    baseCol = Color.Lerp(dark, light, band);

                    if (level >= 14)
                    {
                        float du = Mathf.Abs(u - 0.3f);
                        du = Mathf.Min(du, 1f - du) * 2.4f;
                        float dv = (v - 0.38f) * 4.2f;
                        float spot = du * du + dv * dv;
                        if (spot < 1f)
                        {
                            Color storm = ClampColor(light * 1.15f);
                            baseCol = Color.Lerp(storm, baseCol, spot);
                        }
                    }

                    break;
                }
                case StarForgePlanetTheme.Star:
                {
                    float g = Fbm(u, v, 5.5f, 4, seed);
                    g = Mathf.Clamp01(g * g * 1.5f);

                    Color cool = baseColor * 0.75f;
                    Color hot = Color.Lerp(baseColor, Color.white, 0.65f);
                    baseCol = Color.Lerp(cool, hot, g);

                    if (level >= 17 && level <= 21)
                    {
                        float spot = Fbm(u, v, 3f, 3, seed + 31f);
                        if (spot > 0.78f)
                        {
                            baseCol *= 0.5f;
                        }
                    }

                    emissionCol = baseCol * (0.55f + 0.6f * g);
                    break;
                }
                default:
                {
                    float n = Fbm(u, v, 4f, 3, seed);
                    baseCol = Color.Lerp(Color.black, new Color(0.16f, 0.1f, 0.3f), n * 0.4f);
                    float swirl = n > 0.7f ? (n - 0.7f) * 2.2f : 0f;
                    emissionCol = new Color(0.4f, 0.2f, 0.8f) * swirl;
                    break;
                }
            }

            baseCol = ClampColor(baseCol);
            baseCol.a = 1f;
            emissionCol = ClampColor(emissionCol);
            emissionCol.a = 1f;
        }

        private static float Fbm(float u, float v, float scale, int octaves, float seed)
        {
            float amplitude = 0.5f;
            float frequency = scale;
            float sum = 0f;
            float norm = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Seamless(u, v, frequency, seed + i * 37.7f);
                norm += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return norm > 0f ? sum / norm : 0f;
        }

        private static float Seamless(float u, float v, float scale, float seed)
        {
            float angle = u * Mathf.PI * 2f;
            float nx = seed + Mathf.Cos(angle) * scale * 0.45f;
            float ny = seed + Mathf.Sin(angle) * scale * 0.45f + v * scale;
            return Mathf.PerlinNoise(nx, ny);
        }

        private static Texture2D BuildTexture(string name, Color32[] pixels)
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 2;
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static Color ClampColor(Color color)
        {
            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }

        private static Color32 ToColor32(Color color)
        {
            return new Color32(
                (byte)(Mathf.Clamp01(color.r) * 255f),
                (byte)(Mathf.Clamp01(color.g) * 255f),
                (byte)(Mathf.Clamp01(color.b) * 255f),
                255);
        }
    }
}
