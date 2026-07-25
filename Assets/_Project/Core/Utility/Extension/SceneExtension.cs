using UnityEngine.SceneManagement;

/// <summary>
/// EScene 관련 판정 확장 메서드를 담는 유틸리티입니다.
/// </summary>
public static class SceneExtension
{
    /// <summary>
    /// 플레이어·잠수함이 활성화되는 게임플레이 씬인지 여부입니다.
    /// Stage_1 ~ Stage_6 구간만 해당하며 Ending은 제외합니다.
    /// </summary>
    /// <param name="scene">검사할 씬</param>
    public static bool IsGameplay(this EScene scene)
    {
        string path = SceneManager.GetActiveScene().path;
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(path);
        if (buildIndex == -1)
        {
            return true;
        }
        else
        {
            return scene >= EScene.Stage_1 && scene <= EScene.Stage_6;
        }
    }
}
