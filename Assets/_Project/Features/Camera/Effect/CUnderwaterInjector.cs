using UnityEngine;

/// <summary>
/// 플레이어의 다이브 컴포넌트에 필요 주소를 주입합니다.
/// </summary>
public class CUnderwaterInjector : AMono
{
    [SerializeField] private CUnderwaterEffect _underwaterEffect;

    private void Start()
    {
#if UNITY_EDITOR
        if (_underwaterEffect == null)
        {
            UDebug.Print("CUnderwaterEffect가 할당되지 않았습니다.");
            return;
        }

        GameObject player = CGameManager.Player;
        if (player == null)
        {
            UDebug.Print("플레이어를 찾지 못했습니다.");
            return;
        }
#endif
        var comp = player.GetComponentInChildren<CPlayerDive>();
        if(comp == null)
        {
            UDebug.Print("플레이어의 CPlayerDive 컴포넌트를 찾지 못했습니다.");
            return;
        }

        comp.InjectUnderwaterEffect(_underwaterEffect);
    }
}
