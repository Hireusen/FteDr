using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시스템 전역에서 관리되는 UI 프리팹 목록을 등록하고 검증하는 SO 레지스트리 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CUIRegistrySO_", menuName = "ScriptableObjects/CUIRegistrySO", order = 1)]
public class CUIRegistrySO : ABaseSO
{
    /// <summary>
    /// 개별 UI 타입과 프리팹을 1:1로 매핑하기 위한 데이터 구조체입니다.
    /// </summary>
    [System.Serializable]
    public struct UIMappingData
    {
        public EUI uIType;
        public GameObject uIPrefab;
    }

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("UI Register Settings")]
    [SerializeField] private List<UIMappingData> _uiList = new List<UIMappingData>();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public List<UIMappingData> UIList => _uiList;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>
    /// UI 레지스트리 인스펙터 설정값들의 무결성을 검사하여 에러 목록을 수집합니다.
    /// </summary>
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);

        if (errorList == null || _uiList.Count == 0)
        {
            errorList.Add($"{errorList.Count + 1}. 등록된 UI 매핑 목록이 비어있습니다.");
            return;
        }

        HashSet<EUI> uiTypeSet = new HashSet<EUI>();

        int listCount = _uiList.Count;
        for (int i = 0; i < listCount; i++)
        {
            UIMappingData currentData = _uiList[i];

            if (currentData.uIType == EUI.None)
            {
                errorList.Add($"{errorList.Count + 1}. [{i}]번째 슬롯의 UI Type이 None으로 설정되어 있습니다.");
            }

            if (currentData.uIPrefab == null)
            {
                errorList.Add($"{errorList.Count + 1}. [{i}]번째 슬롯의 UI 프리팹({currentData.uIType})이 누락되었습니다.");
            }

            if (currentData.uIType != EUI.None)
            {
                if (!uiTypeSet.Add(currentData.uIType))
                {
                    errorList.Add($"{errorList.Count + 1}. UI Type '{currentData.uIType}'이 중복 등록되었습니다.");
                }
            }
        }
    }
    #endregion


    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    /// <summary>
    /// 컴포넌트가 인스펙터에 추가되거나 초기화될 때 기본값을 세팅합니다.
    /// </summary>
    protected override void Reset()
    {
        base.Reset();

        _id = "UI_Registry";
        _type = EDataType.UI;
    }
    #endregion
}
