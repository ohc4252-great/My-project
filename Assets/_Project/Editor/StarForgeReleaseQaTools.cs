using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarForge.EditorTools
{
    public static class StarForgeReleaseQaTools
    {
        private const string ScreenshotDirectory =
            "store-assets/screenshots/raw";

        [MenuItem("Star Forge/QA/Clear Local Test Data")]
        public static void ClearLocalTestData()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Star Forge QA",
                "PlayerPrefs에 저장된 게임 진행, 설정, 약관 동의를 모두 삭제합니다.\n" +
                "실제 출시 빌드에는 영향을 주지 않습니다.",
                "초기화",
                "취소");
            if (!confirmed)
            {
                return;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("Star Forge QA: 로컬 테스트 데이터를 초기화했습니다.");
        }

        [MenuItem("Star Forge/QA/Capture Store Screenshot")]
        public static void CaptureStoreScreenshot()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Star Forge QA",
                    "Play Mode에서 실제 게임 화면을 연 뒤 다시 실행하세요.",
                    "확인");
                return;
            }

            string absoluteDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", ScreenshotDirectory));
            Directory.CreateDirectory(absoluteDirectory);

            string fileName =
                "starforge-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
            string absolutePath = Path.Combine(absoluteDirectory, fileName);
            ScreenCapture.CaptureScreenshot(absolutePath);
            Debug.Log("Star Forge QA: 스토어 스크린샷 저장 요청: " + absolutePath);
        }
    }
}
