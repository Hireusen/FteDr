using UnityEngine;

public class CTestStageUnlock : MonoBehaviour
{
    [ContextMenu("Unlock Stage")]
    private void UnlockStage()
    {
        UPlayer.UnlockNextStage();
        USound.PlaySfx(Id.SFX_Sonar_Ping);
        for (int i = 6; i >= 1; --i)
        {
            if (UPlayer.IsStageUnlocked(i))
            {
                UDebug.Print($"{i} 스테이지까지 해금되었습니다.");
                break;
            }
        }
    }
}
