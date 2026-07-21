using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CTestRespawn : AMono
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            CStageManager.Ins.RespawnPlayer();
        }
    }
}
