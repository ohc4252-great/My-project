#if UNITY_EDITOR
using StarForge.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarForge.Editor
{
    public static class StarForgeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/StarForge_MVP.unity";

        [MenuItem("Star Forge/Build MVP Scene")]
        public static void BuildMvpScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject controllerObject = new GameObject("StarForge Game Controller");
            controllerObject.AddComponent<StarForgeGameController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Selection.activeGameObject = controllerObject;
            Debug.Log("StarForge MVP Scene created: " + ScenePath);
        }
    }
}
#endif
