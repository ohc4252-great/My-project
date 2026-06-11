using UnityEngine;

namespace StarForge.Data
{
    public static class StarForgeBalanceLoader
    {
        private const string DefaultResourcePath = "StarForgeBalance";

        public static StarForgeBalance Load(TextAsset overrideAsset)
        {
            TextAsset asset = overrideAsset != null ? overrideAsset : Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                Debug.LogError("StarForgeBalance.json을 Resources 폴더에서 찾을 수 없습니다.");
                return new StarForgeBalance();
            }

            StarForgeBalance balance = JsonUtility.FromJson<StarForgeBalance>(asset.text);
            if (balance == null)
            {
                Debug.LogError("StarForgeBalance.json 파싱에 실패했습니다.");
                return new StarForgeBalance();
            }

            return balance;
        }
    }
}
