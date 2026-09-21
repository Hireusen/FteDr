using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 씬의 오브젝트를 수집하여 환경 설정을 실시간 반영한다.
/// </summary>
public class CApplyShadowOption : AMono
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    [Header("그림자 렌더링 대상")]
    [SerializeField] [ReadOnlyField] private Renderer[] _rendererObjects;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    [ContextMenu("테스트 용도 : 새도우 토글")]
    public void ToggleShadowMode()
    {
        CLocalOptionManager.Ins.SetShadow(!CLocalOptionManager.Ins.Option.useShadow);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 현재 씬의 모든 오브젝트에 새도우 설정 반영
    private void SetShadowCastingMode(ShadowCastingMode shadowCastingMode)
    {
        if (_rendererObjects == null)
        {
            UDebug.Print($"렌더러 오브젝트가 null입니다!", LogType.Error, this);
            return;
        }

        for (int i = 0; i < _rendererObjects.Length; ++i)
        {
            _rendererObjects[i].shadowCastingMode = shadowCastingMode;
        }
    }

    private void OptionShadowChangeHandle(OnOptionShadowChanged ctx)
    {
        SetShadowCastingMode(ctx.shadowCastingMode);
    }
    #endregion

    #region ─────────────────────────▷ 메시지 함수 ◁─────────────────────────
    private void Start()
    {
        _rendererObjects = UObject.FindComponents<Renderer>(true, FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        CEventBus<OnOptionShadowChanged>.Subscribe(OptionShadowChangeHandle);
    }

    private void OnDisable()
    {
        CEventBus<OnOptionShadowChanged>.Unsubscribe(OptionShadowChangeHandle);
    }
    #endregion
}
