using System.IO;
using UnityEditor;
using UnityEditor.Build;
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
            PlayerSettings.productName = "별의 제련소";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.starforge.stellarsmith");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }
    }
}
