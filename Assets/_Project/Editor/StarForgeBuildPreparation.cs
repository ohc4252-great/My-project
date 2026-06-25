using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StarForge.EditorTools
{
    public static class StarForgeBuildPreparation
    {
        private static readonly string[] RequiredShaders =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Sprites/Default"
        };

        private const string ScenePath = "Assets/_Project/Scenes/StarForge_MVP.unity";

        private const string AppIconPath =
            "Assets/_Project/Audio/별강화하기로고-Photoroom.png";
        private const string AppIconBackgroundPath =
            "Assets/_Project/Art/Icon/IconBackgroundBlack.png";

        [MenuItem("Star Forge/Prepare Android Build")]
        public static void PrepareAndroidBuild()
        {
            AddAlwaysIncludedShaders();
            ConfigureScenes();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Star Forge",
                "안드로이드 빌드 준비 완료\n\n" +
                "- 런타임 셰이더를 빌드에 포함했습니다\n" +
                "- 빌드 씬 목록을 설정했습니다\n" +
                "- 세로 고정 / 패키지명 / IL2CPP / ARM64 설정 완료\n\n" +
                "이제 File > Build Profiles에서 Android로 빌드하세요.",
                "확인");
        }

        [MenuItem("Star Forge/Build AAB (Google Play)")]
        public static void BuildAndroidAppBundle()
        {
            // 1) 빌드 준비(셰이더/씬/플레이어 설정)를 먼저 적용한다.
            AddAlwaysIncludedShaders();
            ConfigureScenes();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();

            // 2) 릴리스 서명 키스토어가 설정돼 있는지 확인한다.
            //    설정돼 있지 않으면 디버그 키로 서명되어 Play 콘솔이 거부한다.
            if (!PlayerSettings.Android.useCustomKeystore ||
                string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                EditorUtility.DisplayDialog(
                    "Star Forge",
                    "업로드 키스토어가 설정되어 있지 않습니다.\n\n" +
                    "Project Settings > Player > Publishing Settings에서\n" +
                    "Keystore를 먼저 생성/선택한 뒤 다시 실행하세요.\n\n" +
                    "(키스토어 없이 만든 AAB는 디버그 서명이라 Play 콘솔이 거부합니다.)",
                    "확인");
                return;
            }

            // 3) 빌드할 씬 목록을 구한다(빌드 설정에 등록된 활성 씬, 없으면 기본 씬).
            string[] scenes = GetEnabledScenePaths();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Star Forge",
                    "빌드할 씬이 없습니다.\n" +
                    "Build Profiles에 씬을 추가하거나 " +
                    ScenePath + " 가 존재하는지 확인하세요.",
                    "확인");
                return;
            }

            // 4) 저장 경로를 묻는다.
            string defaultName = MakeSafeFileName(PlayerSettings.productName) + ".aab";
            string outputPath = EditorUtility.SaveFilePanel(
                "AAB 저장 위치",
                "",
                defaultName,
                "aab");
            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }

            // 5) 플랫폼을 Android로 전환하고 App Bundle 출력을 켠다.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Android,
                        BuildTarget.Android))
                {
                    EditorUtility.DisplayDialog(
                        "Star Forge",
                        "Android 플랫폼으로 전환하지 못했습니다.\n" +
                        "Android Build Support 모듈이 설치돼 있는지 확인하세요.",
                        "확인");
                    return;
                }
            }

            EditorUserBuildSettings.buildAppBundle = true;

            // 6) 빌드 실행.
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log(
                    "AAB 빌드 성공: " + outputPath +
                    " (" + sizeMb.ToString("0.0") + " MB, versionCode " +
                    PlayerSettings.Android.bundleVersionCode + ")");
                if (EditorUtility.DisplayDialog(
                        "Star Forge",
                        "AAB 빌드 성공\n\n" +
                        outputPath + "\n" +
                        sizeMb.ToString("0.0") + " MB / versionCode " +
                        PlayerSettings.Android.bundleVersionCode + "\n\n" +
                        "Play 콘솔에 업로드하세요. 재업로드 시 Bundle Version Code를 올려야 합니다.",
                        "폴더 열기",
                        "닫기"))
                {
                    EditorUtility.RevealInFinder(outputPath);
                }
            }
            else
            {
                Debug.LogError(
                    "AAB 빌드 실패: " + summary.result +
                    " (오류 " + summary.totalErrors + "건). Console 로그를 확인하세요.");
                EditorUtility.DisplayDialog(
                    "Star Forge",
                    "AAB 빌드 실패: " + summary.result + "\n" +
                    "Console 창의 오류 로그를 확인하세요.",
                    "확인");
            }
        }

        private static string[] GetEnabledScenePaths()
        {
            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    scenes.Add(scene.path);
                }
            }

            if (scenes.Count == 0 && File.Exists(ScenePath))
            {
                scenes.Add(ScenePath);
            }

            return scenes.ToArray();
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "StarForge";
            }

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private static void AddAlwaysIncludedShaders()
        {
            Object[] graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null || graphicsSettings.Length == 0)
            {
                Debug.LogWarning("GraphicsSettings.asset을 찾을 수 없어 셰이더 포함을 건너뜁니다.");
                return;
            }

            SerializedObject serialized = new SerializedObject(graphicsSettings[0]);
            SerializedProperty shaderList = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (shaderList == null)
            {
                Debug.LogWarning("m_AlwaysIncludedShaders 속성을 찾을 수 없습니다.");
                return;
            }

            foreach (string shaderName in RequiredShaders)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning("셰이더를 찾을 수 없습니다: " + shaderName);
                    continue;
                }

                bool alreadyIncluded = false;
                for (int i = 0; i < shaderList.arraySize; i++)
                {
                    if (shaderList.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (!alreadyIncluded)
                {
                    int index = shaderList.arraySize;
                    shaderList.arraySize++;
                    shaderList.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                    Debug.Log("Always Included Shaders에 추가: " + shaderName);
                }
            }

            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureScenes()
        {
            if (File.Exists(ScenePath))
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                Debug.Log("빌드 씬 설정: " + ScenePath);
            }
            else
            {
                Debug.LogWarning(
                    "StarForge_MVP 씬이 없습니다. Star Forge > Build MVP Scene을 먼저 실행하거나, " +
                    "Build Profiles에서 현재 씬을 직접 추가하세요.");
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "StarForge";
            PlayerSettings.productName = "별 강화하기";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.starforge.stellarsmith");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            ApplyAppIcon();
        }

        // Codemagic(혹은 다른 CI)에서 -executeMethod 로 호출하는 진입점이다.
        // macOS CI에서 Unity가 iOS Xcode 프로젝트를 "ios" 폴더에 생성한다.
        // 대화상자를 쓰지 않으며, 실패 시 종료코드 1로 빌드를 실패시킨다.
        public static void BuildiOSForCI()
        {
            AddAlwaysIncludedShaders();
            ConfigureScenes();
            ConfigureiOSPlayerSettings();
            AssetDatabase.SaveAssets();

            string[] scenes = GetEnabledScenePaths();
            if (scenes.Length == 0)
            {
                Debug.LogError("빌드할 씬이 없습니다. EditorBuildSettings 또는 " + ScenePath + " 확인.");
                EditorApplication.Exit(1);
                return;
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "ios",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("iOS Xcode 프로젝트 생성 성공: ios/");
            }
            else
            {
                Debug.LogError(
                    "iOS 빌드 실패: " + report.summary.result +
                    " (오류 " + report.summary.totalErrors + "건)");
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureiOSPlayerSettings()
        {
            PlayerSettings.companyName = "StarForge";
            PlayerSettings.productName = "별 강화하기";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.starforge.stellarsmith");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            // 참고: iOS App Store 아이콘은 알파(투명)가 없어야 하므로
            // 투명 배경 로고를 그대로 쓰지 않는다. 불투명 아이콘을 준비해
            // Player > iOS > Icon 에서 별도로 지정할 것.
        }

        [MenuItem("Star Forge/Set App Icon")]
        public static void SetAppIconMenu()
        {
            if (ApplyAppIcon())
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(
                    "Star Forge",
                    "앱 아이콘을 설정했습니다:\n" + AppIconPath,
                    "확인");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Star Forge",
                    "앱 아이콘 이미지를 찾지 못했습니다:\n" + AppIconPath,
                    "확인");
            }
        }

        private static bool ApplyAppIcon()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
            {
                Debug.LogWarning("앱 아이콘 이미지를 찾을 수 없어 아이콘 설정을 건너뜁니다: " + AppIconPath);
                return false;
            }

            // 같은 로고를 모든 밀도 슬롯에 채운다. 빌드 시 Unity가 각 해상도로 리사이즈한다.
            SetIconsForKind(IconKind.Application, icon);
            Texture2D blackBackground = EnsureBlackIconBackground();
            if (blackBackground != null)
            {
                SetAdaptiveIcons(icon, blackBackground);
            }

            return true;
        }

        private static Texture2D EnsureBlackIconBackground()
        {
            Texture2D existing =
                AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconBackgroundPath);
            if (existing != null)
            {
                return existing;
            }

            string directory = Path.GetDirectoryName(AppIconBackgroundPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Texture2D texture = new Texture2D(
                108,
                108,
                TextureFormat.RGBA32,
                false);
            Color32[] pixels = new Color32[108 * 108];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(AppIconBackgroundPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(AppIconBackgroundPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                AppIconBackgroundPath);
        }

        private static void SetAdaptiveIcons(
            Texture2D foreground,
            Texture2D background)
        {
            PlatformIcon[] adaptiveIcons =
                PlayerSettings.GetPlatformIcons(
                    NamedBuildTarget.Android,
                    AndroidPlatformIconKind.Adaptive);
            if (adaptiveIcons.Length == 0)
            {
                return;
            }

            for (int i = 0; i < adaptiveIcons.Length; i++)
            {
                adaptiveIcons[i].SetTexture(background, 0);
                adaptiveIcons[i].SetTexture(foreground, 1);
            }

            PlayerSettings.SetPlatformIcons(
                NamedBuildTarget.Android,
                AndroidPlatformIconKind.Adaptive,
                adaptiveIcons);
        }

        private static void SetIconsForKind(IconKind kind, Texture2D icon)
        {
            int sizeCount = PlayerSettings.GetIconSizes(NamedBuildTarget.Android, kind).Length;
            if (sizeCount == 0)
            {
                return;
            }

            Texture2D[] icons = new Texture2D[sizeCount];
            for (int i = 0; i < sizeCount; i++)
            {
                icons[i] = icon;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, kind);
        }
    }
}
