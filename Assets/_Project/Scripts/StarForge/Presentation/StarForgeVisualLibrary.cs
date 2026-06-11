using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarForge.Presentation
{
    public static class StarForgeVisualLibrary
    {
        private static Texture2D softCircleTexture;
        private static Texture2D shockwaveRingTexture;
        private static Texture2D planetRingTexture;
        private static readonly Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

        public static Texture2D SoftCircleTexture
        {
            get
            {
                if (softCircleTexture == null)
                {
                    softCircleTexture = BuildRadialTexture("StarForge Soft Circle", 64, distance =>
                    {
                        float alpha = Mathf.Clamp01(1f - distance);
                        return alpha * alpha;
                    });
                }

                return softCircleTexture;
            }
        }

        public static Texture2D ShockwaveRingTexture
        {
            get
            {
                if (shockwaveRingTexture == null)
                {
                    shockwaveRingTexture = BuildRadialTexture("StarForge Shockwave Ring", 128, distance =>
                    {
                        float band = 1f - Mathf.Abs(distance - 0.7f) / 0.2f;
                        return Mathf.Clamp01(band);
                    });
                }

                return shockwaveRingTexture;
            }
        }

        public static Texture2D PlanetRingTexture
        {
            get
            {
                if (planetRingTexture == null)
                {
                    planetRingTexture = BuildPlanetRingTexture();
                }

                return planetRingTexture;
            }
        }

        private static Mesh[] rockMeshes;

        public static Mesh[] GetRockMeshes()
        {
            if (rockMeshes != null && rockMeshes.Length > 0 && rockMeshes[0] != null)
            {
                return rockMeshes;
            }

            List<Mesh> meshes = new List<Mesh>();
            AddNormalizedRockMeshes(meshes, Resources.LoadAll<Mesh>("Rocks"));
            AddNormalizedRockMeshes(meshes, Resources.LoadAll<Mesh>("Dnk_Dev/RockPack/Models"));

            int proceduralCount = meshes.Count > 0 ? 3 : 5;
            for (int i = 0; i < proceduralCount; i++)
            {
                meshes.Add(BuildRockMesh(i));
            }

            rockMeshes = meshes.ToArray();
            return rockMeshes;
        }

        private static void AddNormalizedRockMeshes(List<Mesh> meshes, Mesh[] sources)
        {
            if (sources == null)
            {
                return;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                Mesh normalized = NormalizeRockMesh(sources[i]);
                if (normalized != null)
                {
                    meshes.Add(normalized);
                }
            }
        }

        private static Mesh NormalizeRockMesh(Mesh source)
        {
            if (source == null)
            {
                return null;
            }

            Vector3[] sourceVertices;
            try
            {
                sourceVertices = source.vertices;
            }
            catch (System.Exception)
            {
                return null;
            }

            if (sourceVertices == null || sourceVertices.Length == 0)
            {
                return null;
            }

            Bounds bounds = source.bounds;
            float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDimension <= 0f)
            {
                return null;
            }

            float scale = 1.05f / maxDimension;
            Vector3 center = bounds.center;
            Vector3[] vertices = new Vector3[sourceVertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (sourceVertices[i] - center) * scale;
            }

            Mesh mesh = new Mesh();
            mesh.name = source.name + " Fragment";
            if (vertices.Length > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv = source.uv;
            mesh.normals = source.normals;
            mesh.triangles = source.triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh GetRandomRockMesh()
        {
            Mesh[] meshes = GetRockMeshes();
            return meshes[Random.Range(0, meshes.Length)];
        }

        private static Mesh BuildRockMesh(int seedIndex)
        {
            System.Random random = new System.Random(seedIndex * 7919 + 13);
            float noiseSeed = seedIndex * 17.31f + 5.7f;
            Vector3 axisScale = new Vector3(
                Mathf.Lerp(0.7f, 1.25f, (float)random.NextDouble()),
                Mathf.Lerp(0.7f, 1.25f, (float)random.NextDouble()),
                Mathf.Lerp(0.7f, 1.25f, (float)random.NextDouble()));

            List<Vector3> baseVertices;
            List<int> baseTriangles;
            BuildIcosphere(out baseVertices, out baseTriangles);

            for (int i = 0; i < baseVertices.Count; i++)
            {
                Vector3 direction = baseVertices[i].normalized;
                float shape = Noise3(direction * 1.7f, noiseSeed);
                float bump = Noise3(direction * 4.6f, noiseSeed + 31f);
                float radius = Mathf.Max(0.25f, 1f + (shape - 0.5f) * 1.05f + (bump - 0.5f) * 0.45f);
                baseVertices[i] = direction * radius;
            }

            int cuts = 2 + seedIndex % 2;
            for (int c = 0; c < cuts; c++)
            {
                Vector3 cutNormal = new Vector3(
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble() * 2f - 1f);
                cutNormal = cutNormal.sqrMagnitude < 0.001f ? Vector3.up : cutNormal.normalized;
                float planeDistance = Mathf.Lerp(0.3f, 0.6f, (float)random.NextDouble());

                for (int i = 0; i < baseVertices.Count; i++)
                {
                    float distance = Vector3.Dot(baseVertices[i], cutNormal);
                    if (distance > planeDistance)
                    {
                        baseVertices[i] -= cutNormal * (distance - planeDistance);
                    }
                }
            }

            for (int i = 0; i < baseVertices.Count; i++)
            {
                baseVertices[i] = Vector3.Scale(baseVertices[i], axisScale) * 0.55f;
            }

            Vector3[] vertices = new Vector3[baseTriangles.Count];
            Vector2[] uv = new Vector2[baseTriangles.Count];
            int[] triangles = new int[baseTriangles.Count];

            for (int i = 0; i < baseTriangles.Count; i++)
            {
                Vector3 vertex = baseVertices[baseTriangles[i]];
                vertices[i] = vertex;
                uv[i] = new Vector2(vertex.x + 0.5f, vertex.y + 0.5f);
                triangles[i] = i;
            }

            Mesh mesh = new Mesh();
            mesh.name = "StarForge Rock " + seedIndex;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void BuildIcosphere(out List<Vector3> vertices, out List<int> triangles)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            vertices = new List<Vector3>
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f)
            };

            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized;
            }

            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            triangles = new List<int>(faces.Length * 4);
            Dictionary<long, int> midpointCache = new Dictionary<long, int>();

            for (int i = 0; i < faces.Length; i += 3)
            {
                int a = faces[i];
                int b = faces[i + 1];
                int c = faces[i + 2];
                int ab = GetMidpoint(vertices, midpointCache, a, b);
                int bc = GetMidpoint(vertices, midpointCache, b, c);
                int ca = GetMidpoint(vertices, midpointCache, c, a);

                triangles.Add(a); triangles.Add(ab); triangles.Add(ca);
                triangles.Add(b); triangles.Add(bc); triangles.Add(ab);
                triangles.Add(c); triangles.Add(ca); triangles.Add(bc);
                triangles.Add(ab); triangles.Add(bc); triangles.Add(ca);
            }
        }

        private static int GetMidpoint(List<Vector3> vertices, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            int index;
            if (cache.TryGetValue(key, out index))
            {
                return index;
            }

            Vector3 midpoint = ((vertices[a] + vertices[b]) * 0.5f).normalized;
            vertices.Add(midpoint);
            index = vertices.Count - 1;
            cache[key] = index;
            return index;
        }

        private static float Noise3(Vector3 point, float seed)
        {
            float xy = Mathf.PerlinNoise(point.x + seed, point.y + seed);
            float yz = Mathf.PerlinNoise(point.y - seed, point.z + seed);
            float zx = Mathf.PerlinNoise(point.z + seed, point.x - seed);
            return (xy + yz + zx) / 3f;
        }

        public static int GetLevelTier(int level)
        {
            if (level >= 29)
            {
                return 4;
            }

            if (level >= 28)
            {
                return 3;
            }

            if (level >= 20)
            {
                return 2;
            }

            if (level >= 10)
            {
                return 1;
            }

            return 0;
        }

        public static Material CreateParticleMaterial(Color tint, bool additive, Texture2D texture = null)
        {
            Shader shader = FindParticleShader();
            Material material = new Material(shader);
            material.name = additive ? "StarForge Additive" : "StarForge Alpha";

            Texture2D map = texture != null ? texture : SoftCircleTexture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", map);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", map);
            }

            SetMaterialColor(material, tint);
            SetupTransparency(material, additive);
            return material;
        }

        public static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        public static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            Mesh mesh;
            if (primitiveMeshes.TryGetValue(type, out mesh) && mesh != null)
            {
                return mesh;
            }

            GameObject temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            primitiveMeshes[type] = mesh;
            return mesh;
        }

        public static Mesh CreateRingMesh(float innerRadius, float outerRadius, int segments, float uvRepeat)
        {
            int vertexCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
                vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
                normals[i * 2] = Vector3.up;
                normals[i * 2 + 1] = Vector3.up;
                uv[i * 2] = new Vector2(t * uvRepeat, 0f);
                uv[i * 2 + 1] = new Vector2(t * uvRepeat, 1f);
            }

            for (int i = 0; i < segments; i++)
            {
                int baseIndex = i * 6;
                int v = i * 2;
                triangles[baseIndex] = v;
                triangles[baseIndex + 1] = v + 1;
                triangles[baseIndex + 2] = v + 2;
                triangles[baseIndex + 3] = v + 1;
                triangles[baseIndex + 4] = v + 3;
                triangles[baseIndex + 5] = v + 2;
            }

            Mesh mesh = new Mesh();
            mesh.name = "StarForge Ring";
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Shader FindParticleShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            return shader;
        }

        private static void SetupTransparency(Material material, bool additive)
        {
            material.SetOverrideTag("RenderType", "Transparent");

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", additive ? 2f : 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_SrcBlendAlpha"))
            {
                material.SetInt("_SrcBlendAlpha", additive ? (int)BlendMode.Zero : (int)BlendMode.One);
            }

            if (material.HasProperty("_DstBlendAlpha"))
            {
                material.SetInt(
                    "_DstBlendAlpha",
                    additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent + (additive ? 50 : 0);
        }

        private static Texture2D BuildRadialTexture(string name, int size, System.Func<float, float> alphaByDistance)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(alphaByDistance(distance));
                    byte a = (byte)(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static Texture2D BuildPlanetRingTexture()
        {
            const int width = 256;
            const int height = 64;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            texture.name = "StarForge Planet Ring";
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                float edge = Mathf.Clamp01(Mathf.Sin(v * Mathf.PI) * 1.3f);
                float band = 0.55f + 0.45f * Mathf.PerlinNoise(3.1f, v * 11f);
                float gap = Mathf.PerlinNoise(7.7f, v * 23f);
                if (gap < 0.32f)
                {
                    band *= 0.25f;
                }

                float innerBoost = 1.35f - v * 0.7f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float streak = 0.9f + 0.1f * Mathf.PerlinNoise(u * 30f, v * 5f);
                    float alpha = Mathf.Clamp01(edge * band * streak);
                    float brightness = Mathf.Clamp01((0.7f + 0.3f * band) * innerBoost);
                    byte c = (byte)(brightness * 255f);
                    pixels[y * width + x] = new Color32(c, c, c, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }
    }

    public sealed class StarForgeBillboard : MonoBehaviour
    {
        public float depthOffset;

        private void LateUpdate()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            transform.rotation = mainCamera.transform.rotation;

            if (transform.parent != null)
            {
                transform.position = transform.parent.position + mainCamera.transform.forward * depthOffset;
            }
        }
    }

    public sealed class StarForgeOrbitRotator : MonoBehaviour
    {
        public Vector3 degreesPerSecond;

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
