using System.Collections.Generic;
using StarForge.Core;
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
        /// <summary>레퍼런스 이미지에서 추출한 텍스처인지 여부 (얼굴/발광 처리 분기용)</summary>
        public bool isImageBased;
        /// <summary>앞면 언랩 텍스처라 얼굴이 이미 그려져 있는지 여부</summary>
        public bool hasBakedFace;
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

            return StarForgePlanetTheme.Star;
        }

        public static StarForgePlanetSurface Get(int level, Color baseColor)
        {
            return Get(level, baseColor, StarForgePlanetShape.Default);
        }

        public static StarForgePlanetSurface Get(int level, Color baseColor, StarForgePlanetShape shape)
        {
            int key = (int)shape * 1000 + level;
            StarForgePlanetSurface surface;
            if (cache.TryGetValue(key, out surface) && surface != null && surface.baseMap != null)
            {
                return surface;
            }

            surface = LoadImageSurface(level, shape) ?? Generate(level, baseColor);
            cache[key] = surface;
            return surface;
        }

        /// <summary>레퍼런스 이미지에서 추출해 둔 구면 텍스처를 우선 사용합니다. (Resources/PlanetSurfaces/D|H|C{1~27})</summary>
        private static StarForgePlanetSurface LoadImageSurface(int level, StarForgePlanetShape shape)
        {
            string prefix = GetShapePrefix(shape);
            Texture2D texture = Resources.Load<Texture2D>("PlanetSurfaces/" + prefix + Mathf.Max(1, level));
            if (texture == null)
            {
                return null;
            }

            StarForgePlanetSurface surface = new StarForgePlanetSurface();
            surface.theme = GetTheme(level);
            surface.baseMap = texture;
            surface.emissionMap = surface.theme == StarForgePlanetTheme.Lava
                || surface.theme == StarForgePlanetTheme.Star
                ? texture
                : null;
            surface.isImageBased = true;
            surface.hasBakedFace = false;
            return surface;
        }

        /// <summary>블랙홀(28~30) 정면 데칼 텍스처. 없으면 null.</summary>
        public static Texture2D GetBlackHoleFace(int level, StarForgePlanetShape shape)
        {
            if (level < 28)
            {
                return null;
            }

            return Resources.Load<Texture2D>("PlanetSurfaces/BH" + GetShapePrefix(shape) + level);
        }

        private static string GetShapePrefix(StarForgePlanetShape shape)
        {
            return shape == StarForgePlanetShape.Heart
                ? "H"
                : shape == StarForgePlanetShape.Cat ? "C" : "D";
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

                    if (level <= 0)
                    {
                        // 우주 먼지: 곱고 흐릿한 입자 덩어리
                        float fine = Fbm(u, v, 12f, 2, seed + 5f);
                        baseCol = baseColor * (0.62f + 0.3f * n + 0.18f * fine);
                        baseCol = Color.Lerp(baseCol, new Color(0.6f, 0.62f, 0.7f), 0.25f);
                        break;
                    }

                    baseCol = baseColor * (0.5f + 0.55f * n);
                    if (detail > 0.62f)
                    {
                        baseCol *= 0.8f;
                    }

                    if (level <= 4)
                    {
                        // 운석 단계: 충돌 크레이터가 뚜렷하게
                        ApplyCraters(ref baseCol, u, v, seed, 3 + level * 2, 0.035f, 0.085f);
                    }
                    else
                    {
                        // 소행성 단계: 크레이터는 줄고 광맥이 드러남
                        ApplyCraters(ref baseCol, u, v, seed, 4, 0.025f, 0.05f);
                        if (level >= 6)
                        {
                            float ridge = 1f - Mathf.Abs(2f * Fbm(u, v, 5f, 3, seed + 77f) - 1f);
                            float vein = Mathf.InverseLerp(0.86f, 0.97f, ridge);
                            if (vein > 0f)
                            {
                                Color metal = level >= 7
                                    ? new Color(1f, 0.85f, 0.45f)
                                    : new Color(0.8f, 0.76f, 0.7f);
                                baseCol = Color.Lerp(baseCol, metal, vein * 0.55f);
                            }
                        }
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
                        // 깊은 빙하 균열: 푸른 속살이 비치도록
                        float depth = Mathf.InverseLerp(0.68f, 0.85f, crack);
                        baseCol = Color.Lerp(baseCol * 0.86f, new Color(0.25f, 0.55f, 0.8f), depth * 0.5f);
                    }

                    float polar = Mathf.Abs(v - 0.5f) * 2f;
                    if (polar > 0.78f)
                    {
                        baseCol = Color.Lerp(baseCol, Color.white, Mathf.InverseLerp(0.78f, 0.95f, polar) * 0.6f);
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
                    ShadeStar(level, u, v, baseColor, seed, out baseCol, out emissionCol);
                    break;
                }
                default:
                {
                    // 블랙홀 28~30: 갈수록 어둡고, 빛은 가장자리로 응축
                    float n = Fbm(u, v, 4f, 3, seed);
                    if (level >= 30)
                    {
                        // 특이점: 거의 완전한 어둠 + 푸른 빛 테두리 잔광
                        baseCol = Color.Lerp(Color.black, new Color(0.05f, 0.04f, 0.1f), n * 0.35f);
                        float rim = Mathf.Pow(Mathf.Clamp01((n - 0.62f) / 0.38f), 2f);
                        emissionCol = new Color(0.55f, 0.7f, 1f) * rim * 0.5f;
                    }
                    else if (level == 29)
                    {
                        // 초대질량 블랙홀: 어둠 속에 휘감기는 보랏빛 광류
                        float streak = Mathf.Sin((u + n * 0.3f) * Mathf.PI * 8f) * 0.5f + 0.5f;
                        baseCol = Color.Lerp(Color.black, new Color(0.1f, 0.07f, 0.2f), n * 0.45f);
                        emissionCol = new Color(0.45f, 0.3f, 0.9f) * Mathf.Pow(streak, 6f) * n * 0.65f;
                    }
                    else
                    {
                        baseCol = Color.Lerp(Color.black, new Color(0.16f, 0.1f, 0.3f), n * 0.4f);
                        float swirl = n > 0.68f ? (n - 0.68f) * 2.4f : 0f;
                        emissionCol = new Color(0.5f, 0.25f, 0.9f) * swirl;
                    }

                    break;
                }
            }

            baseCol = ClampColor(baseCol);
            baseCol.a = 1f;
            emissionCol = ClampColor(emissionCol);
            emissionCol.a = 1f;
        }

        private static void ShadeStar(
            int level,
            float u,
            float v,
            Color baseColor,
            float seed,
            out Color baseCol,
            out Color emissionCol)
        {
            switch (level)
            {
                case 16: // 원시별: 휘감기는 먼지와 갓 점화된 빛
                {
                    float n = Fbm(u, v, 3.4f, 4, seed);
                    float swirlU = u + 0.22f * (n - 0.5f);
                    float band = Mathf.Sin((v + 0.3f * (n - 0.5f)) * Mathf.PI * 5f + swirlU * Mathf.PI * 2f) * 0.5f + 0.5f;
                    Color dust = baseColor * 0.45f;
                    Color hot = Color.Lerp(baseColor, Color.white, 0.4f);
                    baseCol = Color.Lerp(dust, hot, band * 0.6f + n * 0.4f);
                    emissionCol = baseCol * (0.4f + 0.5f * band);
                    break;
                }
                case 17: // 적색왜성: 느리게 끓는 깊은 붉은 대류 세포
                {
                    float cells = Fbm(u, v, 2.6f, 3, seed);
                    float grain = Fbm(u, v, 9f, 2, seed + 17f);
                    Color cool = new Color(0.45f, 0.08f, 0.04f);
                    Color warm = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.2f), 0.5f);
                    baseCol = Color.Lerp(cool, warm, cells * 0.75f + grain * 0.25f);
                    emissionCol = baseCol * (0.45f + 0.45f * cells);
                    break;
                }
                case 18: // 주계열성: 황금빛 쌀알 조직과 흑점
                {
                    float g = Fbm(u, v, 8f, 3, seed);
                    Color coolBody = baseColor * 0.8f;
                    Color hotBody = Color.Lerp(baseColor, Color.white, 0.7f);
                    baseCol = Color.Lerp(coolBody, hotBody, g);
                    float spot = Fbm(u, v, 2.8f, 3, seed + 31f);
                    if (spot > 0.74f)
                    {
                        baseCol *= Mathf.Lerp(1f, 0.35f, Mathf.InverseLerp(0.74f, 0.88f, spot));
                    }

                    emissionCol = baseCol * (0.6f + 0.5f * g);
                    break;
                }
                case 19: // 청색거성: 매끈하고 차가운 청백색 광휘
                {
                    float sheen = Fbm(u, v, 5f, 3, seed);
                    Color core = Color.Lerp(baseColor, Color.white, 0.55f);
                    baseCol = Color.Lerp(baseColor * 0.85f, core, 0.5f + 0.5f * sheen);
                    emissionCol = baseCol * (0.75f + 0.35f * sheen);
                    break;
                }
                case 20: // 적색거성: 거대한 느린 대류 무늬
                {
                    float cells = Fbm(u, v, 1.9f, 3, seed);
                    float detail = Fbm(u, v, 6f, 2, seed + 7f);
                    Color dark = new Color(0.5f, 0.1f, 0.03f);
                    Color bright = Color.Lerp(baseColor, new Color(1f, 0.62f, 0.25f), 0.6f);
                    baseCol = Color.Lerp(dark, bright, cells * 0.8f + detail * 0.2f);
                    emissionCol = baseCol * (0.5f + 0.5f * cells);
                    break;
                }
                case 21: // 초거성: 황금빛 화염 필라멘트
                {
                    float r = Fbm(u, v, 3.4f, 4, seed);
                    float ridge = 1f - Mathf.Abs(2f * r - 1f);
                    float flare = Mathf.Pow(Mathf.Clamp01(ridge), 3f);
                    Color body = Color.Lerp(new Color(0.85f, 0.4f, 0.08f), baseColor, 0.5f);
                    Color hotFlare = Color.Lerp(baseColor, Color.white, 0.65f);
                    baseCol = Color.Lerp(body, hotFlare, flare);
                    emissionCol = baseCol * (0.55f + 0.6f * flare);
                    break;
                }
                case 22: // 백색왜성: 결정처럼 차분하고 순수한 백광
                {
                    float grain = Fbm(u, v, 10f, 2, seed);
                    float lattice = 1f - Mathf.Abs(2f * Fbm(u, v, 5.5f, 2, seed + 23f) - 1f);
                    baseCol = Color.Lerp(new Color(0.82f, 0.88f, 1f), Color.white, 0.6f + 0.4f * grain);
                    if (lattice > 0.88f)
                    {
                        baseCol = Color.Lerp(baseCol, new Color(0.75f, 0.9f, 1f), 0.4f);
                    }

                    emissionCol = baseCol * (0.8f + 0.25f * grain);
                    break;
                }
                case 23: // 중성자별: 극도로 매끈한 표면 위 가는 광선 라인
                case 24: // 펄서: 중성자별 + 극지 핫스팟
                {
                    float surface = Fbm(u, v, 6f, 2, seed);
                    Color body = Color.Lerp(baseColor, Color.white, 0.5f + 0.2f * surface);
                    float line = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 22f));
                    float lineGlow = Mathf.Pow(line, 24f) * 0.5f;
                    baseCol = body;
                    emissionCol = body * (0.75f + 0.3f * surface) + Color.white * lineGlow * 0.4f;

                    if (level == 24)
                    {
                        float polar = Mathf.Abs(v - 0.5f) * 2f;
                        if (polar > 0.72f)
                        {
                            float cap = Mathf.InverseLerp(0.72f, 0.95f, polar);
                            emissionCol += Color.Lerp(baseColor, Color.white, 0.6f) * cap * 1.1f;
                            baseCol = Color.Lerp(baseCol, Color.white, cap * 0.5f);
                        }
                    }

                    break;
                }
                case 25: // 마그네타: 보랏빛 자기장 필라멘트가 표면을 휘감음
                {
                    float r = Fbm(u, v, 4.2f, 4, seed);
                    float ridge = 1f - Mathf.Abs(2f * r - 1f);
                    float field = Mathf.Pow(Mathf.Clamp01(ridge), 4f);
                    Color deep = new Color(0.2f, 0.08f, 0.4f);
                    Color glow = Color.Lerp(baseColor, new Color(0.9f, 0.6f, 1f), 0.5f);
                    baseCol = Color.Lerp(deep, glow, 0.35f + field * 0.65f);
                    emissionCol = glow * (0.35f + field * 1.1f);
                    break;
                }
                case 26: // 초신성 잔해: 어둠 속에 퍼지는 분홍·주황 필라멘트 성운
                {
                    float f = Fbm(u, v, 3.8f, 4, seed);
                    float filament = Mathf.Pow(1f - Mathf.Abs(2f * f - 1f), 3.2f);
                    float patch = Fbm(u, v, 2.2f, 3, seed + 41f);
                    Color space = new Color(0.05f, 0.03f, 0.09f);
                    Color pink = Color.Lerp(baseColor, new Color(1f, 0.55f, 0.75f), 0.5f);
                    Color amber = new Color(1f, 0.65f, 0.3f);
                    Color fil = Color.Lerp(pink, amber, patch);
                    baseCol = Color.Lerp(space, fil, filament * 0.9f);
                    emissionCol = fil * filament * (0.5f + 0.6f * patch);
                    break;
                }
                case 27: // 쿼크별: 일렁이는 보랏빛 초고밀도 광택
                {
                    float shimmer = Fbm(u, v, 11f, 3, seed);
                    float wave = Mathf.Sin((u * 2f + v) * Mathf.PI * 6f + shimmer * 4f) * 0.5f + 0.5f;
                    Color core = Color.Lerp(baseColor, Color.white, 0.35f + 0.3f * shimmer);
                    Color deep = baseColor * 0.55f;
                    baseCol = Color.Lerp(deep, core, 0.4f + 0.6f * wave * shimmer);
                    emissionCol = baseCol * (0.7f + 0.45f * wave);
                    break;
                }
                default: // 안전망: 기존 일반 별 셰이딩
                {
                    float g = Fbm(u, v, 5.5f, 4, seed);
                    g = Mathf.Clamp01(g * g * 1.5f);
                    Color cool = baseColor * 0.75f;
                    Color hot = Color.Lerp(baseColor, Color.white, 0.65f);
                    baseCol = Color.Lerp(cool, hot, g);
                    emissionCol = baseCol * (0.55f + 0.6f * g);
                    break;
                }
            }
        }

        private static void ApplyCraters(
            ref Color baseCol,
            float u,
            float v,
            float seed,
            int count,
            float radiusMin,
            float radiusMax)
        {
            for (int i = 0; i < count; i++)
            {
                float cu = Hash(seed + i * 3.7f);
                float cv = Mathf.Lerp(0.12f, 0.88f, Hash(seed + i * 9.1f + 1.3f));
                float radius = Mathf.Lerp(radiusMin, radiusMax, Hash(seed + i * 5.3f + 2.6f));

                float du = Mathf.Abs(u - cu);
                du = Mathf.Min(du, 1f - du);
                float dv = v - cv;
                float distance = Mathf.Sqrt(du * du * 4f + dv * dv);

                if (distance < radius)
                {
                    // 크레이터 내부: 중심으로 갈수록 어둡게
                    float t = distance / radius;
                    float floor = Mathf.SmoothStep(0.55f, 1f, t);
                    baseCol *= Mathf.Lerp(0.62f, 1f, floor);
                }
                else if (distance < radius * 1.18f)
                {
                    // 크레이터 림: 살짝 밝게 솟은 테두리
                    float rim = 1f - Mathf.InverseLerp(radius, radius * 1.18f, distance);
                    baseCol *= 1f + rim * 0.22f;
                }
            }
        }

        private static float Hash(float n)
        {
            float value = Mathf.Sin(n * 127.1f) * 43758.5453f;
            return value - Mathf.Floor(value);
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
