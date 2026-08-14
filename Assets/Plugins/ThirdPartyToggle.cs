using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.ThirdParty
{
    public static class ThirdPartyToggle
    {
        private const string THIRDPARTY_NAME = "ThirdParty";
        // 수정할 필요 X
        private const string NORMAL_PATH = "Assets/" + THIRDPARTY_NAME;
        private const string HIDDEN_PATH = NORMAL_PATH + "~";
        private const string META = ".meta";

        [MenuItem("Tools/서드파티/숨김")]
        public static void DisableFolder()
        {
            string normalFullPath = Path.GetFullPath(NORMAL_PATH);
            string hiddenFullPath = Path.GetFullPath(HIDDEN_PATH);
            // 일반 폴더가 존재하는가?
            if (!Directory.Exists(normalFullPath))
            {
                Debug.LogWarning("[ThirdParty] 활성화된 서드파티 폴더를 찾을 수 없습니다.");
                return;
            }

            // 메타 파일이 존재하는가?
            string metaFullPath = normalFullPath + META;
            string hiddenMetaFullPath = metaFullPath + "~";
            if (!File.Exists(metaFullPath))
            {
                Debug.LogWarning("[ThirdParty] 활성화된 메타 파일을 찾을 수 없습니다.");
                return;
            }

            // 이동
            Directory.Move(normalFullPath, hiddenFullPath);
            File.Move(metaFullPath, hiddenMetaFullPath);
            AssetDatabase.Refresh();
            Debug.Log("[ThirdParty] 서드파티 폴더를 숨겼습니다.");
        }

        [MenuItem("Tools/서드파티/활성화")]
        public static void EnableFolder()
        {
            string normalFullPath = Path.GetFullPath(NORMAL_PATH);
            string hiddenFullPath = Path.GetFullPath(HIDDEN_PATH);
            // 숨김 폴더가 존재하는가?
            if (!Directory.Exists(hiddenFullPath))
            {
                Debug.LogWarning("[ThirdParty] 숨겨진 서드파티 폴더를 찾을 수 없습니다.");
                return;
            }

            // 메타 파일이 존재하는가?
            string metaFullPath = normalFullPath + META;
            string hiddenMetaFullPath = metaFullPath + "~";
            if (!File.Exists(hiddenMetaFullPath))
            {
                Debug.LogWarning("[ThirdParty] 숨겨진 메타 파일을 찾을 수 없습니다.");
                return;
            }

            // 이동
            Directory.Move(hiddenFullPath, normalFullPath);
            File.Move(hiddenMetaFullPath, metaFullPath);
            AssetDatabase.Refresh();
            Debug.Log("[ThirdParty] 서드파티 폴더를 활성화했습니다.");
        }
    }
}
