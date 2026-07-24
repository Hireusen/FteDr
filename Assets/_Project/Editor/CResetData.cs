#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 게임 진행 상황을 초기화합니다.
/// </summary>
[CustomEditor(typeof(CSoundSO))]
public class CResetData : Editor
{
    [MenuItem("Tools/데이터 리셋")]
    public static void Execute()
    {
        USaveFile.DeleteAll();
    }
}
#endif
