using StarForge.Data;
using UnityEngine;

namespace StarForge.Presentation
{
    public sealed class StarForgeCollectionPreview : MonoBehaviour
    {
        private const int PreviewLayer = 30;
        private const int TextureSize = 768;

        private Camera previewCamera;
        private StarForgePlanetView planetView;
        private RenderTexture renderTexture;

        public Texture OutputTexture
        {
            get
            {
                EnsureCreated();
                return renderTexture;
            }
        }

        public void Show(StageVisualConfig stage)
        {
            if (stage == null)
            {
                return;
            }

            EnsureCreated();
            gameObject.SetActive(true);
            StageVisualConfig previewStage = new StageVisualConfig();
            previewStage.level = stage.level;
            previewStage.displayName = stage.displayName;
            previewStage.color = stage.color;
            previewStage.scale = 2.2f;
            previewStage.emission = stage.emission;
            previewStage.rotationSpeed = stage.rotationSpeed;
            planetView.ApplyStage(previewStage);
            SetLayerRecursively(gameObject, PreviewLayer);
            previewCamera.enabled = true;
        }

        public void Hide()
        {
            if (previewCamera != null)
            {
                previewCamera.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (renderTexture == null)
            {
                return;
            }

            renderTexture.Release();
            Destroy(renderTexture);
        }

        private void EnsureCreated()
        {
            if (previewCamera != null)
            {
                return;
            }

            transform.position = new Vector3(1000f, 0f, 0f);

            renderTexture = new RenderTexture(
                TextureSize,
                TextureSize,
                24,
                RenderTextureFormat.ARGB32);
            renderTexture.name = "StarForge Collection Preview";
            renderTexture.antiAliasing = 2;
            renderTexture.Create();

            GameObject cameraObject = new GameObject("Collection Preview Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.05f, -6f);
            cameraObject.transform.localRotation = Quaternion.identity;
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.012f, 0.018f, 0.035f, 1f);
            previewCamera.fieldOfView = 38f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 30f;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.targetTexture = renderTexture;
            previewCamera.enabled = false;

            GameObject lightObject = new GameObject("Collection Preview Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(32f, -28f, 0f);
            Light keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.cullingMask = 1 << PreviewLayer;

            GameObject planetObject = new GameObject("Collection Planet");
            planetObject.transform.SetParent(transform, false);
            planetObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            planetView = planetObject.AddComponent<StarForgePlanetView>();
            planetView.SetDecorScaleMultiplier(0.62f);

            SetLayerRecursively(gameObject, PreviewLayer);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            Transform targetTransform = target.transform;
            for (int i = 0; i < targetTransform.childCount; i++)
            {
                SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
            }
        }
    }
}
