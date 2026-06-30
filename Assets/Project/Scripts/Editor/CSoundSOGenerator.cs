using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 내부에 존재하는 모든 AudioClip을 일괄 스캔하여, SoundSO 에셋을 자동 생성합니다.
/// </summary>
public class CSoundSOGenerator : EditorWindow
{
    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    [MenuItem("Tools/사운드 SO 생성기")]
    public static void ShowWindow()
    {
        GetWindow<CSoundSOGenerator>("Sound SO Generator");
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private static void Entry()
    {
        string[] importPath = { K.SOUND_IMPORT_PATH };
        string exportPath = K.SOUND_EXPORT_PATH;
        // 오디오 폴더에서 AudioClip 타입의 에셋 GUID를 모두 수집
        string[] guids = AssetDatabase.FindAssets($"t:AudioClip", importPath);
        if (guids == null || guids.Length == 0)
        {
            UDebug.Print($"오디오 폴더에 오디오 클립이 존재하지 않습니다.", LogType.Warning);
            return;
        }

        // 폴더의 존재 보장
        string ioSavePath = Path.GetFullPath(exportPath);
        if (!Directory.Exists(ioSavePath))
        {
            Directory.CreateDirectory(ioSavePath);
        }

        // 클립 순회
        LoopAudioClips(guids, exportPath);
    }

    // 검색된 오디오 클립들의 GUID 배열을 순회하며 하나씩 SO 생성 메서드로 전달합니다.
    private static void LoopAudioClips(string[] guids, string exportPath)
    {
        int length = guids.Length;
        int success = 0;
        int skip = 0;

        for (int i = 0; i < length; ++i)
        {
            string guid = guids[i];

            // GUID를 물리적 에셋 경로로 변환하여 AudioClip 인스턴스 로드
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            // 실행되면 이상함
            if (clip == null)
            {
                UDebug.Print($"{path}에 오디오 클립이 존재하지 않는 알 수 없는 오류 발생", LogType.Assert);
                continue;
            }

            // 로드된 클립을 기반으로 SO 파일 생성 시도
            if (CreateSoundSO(clip, exportPath))
            {
                success++;
            }
            else
            {
                skip++;
            }
        }

        // 일괄 저장 및 에디터 새로고침
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UDebug.Print($"사운드 SO {skip}개를 보존했으며, 사운드 SO {success}개를 새로 생성했습니다.");
    }

    // SoundSO 인스턴스 생성 및 저장
    private static bool CreateSoundSO(AudioClip clip, string exportPath)
    {
        string savePath = string.Format("{0}/SoundSO_{1}.asset", exportPath, clip.name);
        // 중복 생성 방지
        if (File.Exists(savePath)) return false;

        // 생성 & 초기화 & 저장
        CSoundSO so = ScriptableObject.CreateInstance<CSoundSO>();
        so.InitSO(clip.name, clip);
        AssetDatabase.CreateAsset(so, savePath);
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnGUI()
    {
        GUILayout.Space(10);
        if (GUILayout.Button("사운드 SO 일괄 생성하기", GUILayout.Height(30)))
        {
            Entry();
        }
    }
    #endregion
}
