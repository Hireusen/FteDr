using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 루트에 부착하는 장비 코디네이터입니다.
/// 자식 계층의 모든 <see cref="AGear"/>를 모아 일괄 가동/정지하거나 타입으로 조회합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CPlayerEquipment : AMono
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private AGear[] _gears; // 자식 계층의 모든 장비 (비활성 포함)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 플레이어가 가진 모든 장비입니다. (읽기 전용)</summary>
    public IReadOnlyList<AGear> Gears => _gears;

    /// <summary>지정한 타입의 장비를 반환합니다. 없으면 null.</summary>
    /// <typeparam name="T">찾을 장비 컴포넌트 타입</typeparam>
    public T Get<T>() where T : AGear
    {
        if (_gears == null) return null;

        for (int i = 0; i < _gears.Length; ++i)
        {
            if (_gears[i] is T gear)
            {
                return gear;
            }
        }
        return null;
    }

    /// <summary>지정한 데이터 타입의 장비를 반환합니다. 없으면 null.</summary>
    /// <param name="type">장비 데이터 타입</param>
    public AGear Get(EDataType type)
    {
        if (_gears == null) return null;

        for (int i = 0; i < _gears.Length; ++i)
        {
            if (_gears[i] != null && _gears[i].GearType == type)
            {
                return _gears[i];
            }
        }
        return null;
    }

    /// <summary>모든 장비를 가동합니다. (잠수 시작 등)</summary>
    public void ActivateAll()
    {
        if (_gears == null) return;

        for (int i = 0; i < _gears.Length; ++i)
        {
            if (_gears[i] != null) _gears[i].Activate();
        }
    }

    /// <summary>모든 장비를 정지합니다. (귀환·사망 등)</summary>
    public void DeactivateAll()
    {
        if (_gears == null) return;

        for (int i = 0; i < _gears.Length; ++i)
        {
            if (_gears[i] != null) _gears[i].Deactivate();
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // 비활성 오브젝트의 장비까지 모두 수집합니다.
        _gears = GetComponentsInChildren<AGear>(true);
    }
    #endregion
}
