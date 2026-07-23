using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점의 잠수함 업그레이드 전용 데이터입니다.
/// 실제 게임 로직(다음 스테이지 이동 가능 여부)은 UPlayer.UnlockedStage/UnlockNextStage가 전담하고,
/// 사용법: _upgradeCosts[0] = 1000 -> 2스테이지 해금 비용, _upgradeCosts[1] = 다음 비용 -> 3스테이지 해금 비용
/// UData를 거치지 않고, CShopUpgradeRow의 잠수함 로우 인스펙터에 이 SO 에셋을 직접 연결해서 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "SubmarineSO_", menuName = "ScriptableObjects/SubmarineSO", order = 1)]
public class CSubmarineSO : AGearSO
{
    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Submarine) errorList.Add($"{errorList.Count + 1}. 타입이 Submarine이 아닙니다.");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Submarine;
    }
    #endregion
}
