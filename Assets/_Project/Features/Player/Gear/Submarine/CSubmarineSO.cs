using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 연구(해금) 상한을 정의하는 데이터입니다.
/// 스테이지 해금은 골드가 아니라 특수 수집품을 소모하는 연구로 바뀌었으므로, _upgradeCosts의 '값'은 쓰지 않습니다.
/// 대신 배열 길이가 연구 가능 횟수를 정합니다. (MaxLevel = 길이 + 1 = 도달 가능한 최대 스테이지)
/// 예: 항목 2개 -> 최대 3스테이지까지 연구 가능. 스테이지를 늘리려면 항목만 추가하면 됩니다.
/// 실제 게임 로직(연구 가능 여부/해금)은 UStageResearch가 전담하고, UData.Submarine()으로 조회됩니다.
/// </summary>
[CreateAssetMenu(fileName = "SubmarineSO_", menuName = "ScriptableObjects/SubmarineSO", order = 1)]
public class CSubmarineSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("목적지 이름 (연구창에 표시)")]
    [Tooltip("스테이지 순서대로 입력합니다. 0번 = 스테이지 1의 이름. 연구 가능 횟수(_upgradeCosts 길이 + 1)만큼 채워주세요.")]
    [SerializeField] protected string[] _destinationNames;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 단계(스테이지)의 목적지 이름을 반환합니다. 비어있거나 범위를 벗어나면 빈 문자열입니다.
    /// </summary>
    /// <param name="level">스테이지 단계 (1부터 시작)</param>
    public string DestinationName(int level)
        => TryGetArrayValue(_destinationNames, level, out string name) ? name : string.Empty;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Submarine) errorList.Add($"{errorList.Count + 1}. 타입이 Submarine이 아닙니다.");

        // 목적지 이름은 도달 가능한 모든 단계(MaxLevel)만큼 있어야 연구창이 빈칸 없이 표시된다.
        if (_destinationNames == null || _destinationNames.Length < MaxLevel)
        {
            int count = _destinationNames != null ? _destinationNames.Length : 0;
            errorList.Add($"{errorList.Count + 1}. 목적지 이름이 {count}개뿐입니다. (최대 단계 {MaxLevel}개만큼 필요)");
        }
        else
        {
            for (int i = 0; i < MaxLevel; ++i)
            {
                if (_destinationNames[i].IsNotBlank()) continue;
                errorList.Add($"{errorList.Count + 1}. 목적지 이름 {i}번째(스테이지 {i + 1})가 비어있습니다.");
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Submarine;
    }
    #endregion
}
