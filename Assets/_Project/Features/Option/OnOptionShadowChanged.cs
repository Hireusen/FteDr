using UnityEngine.Rendering;

/// <summary>
/// 그림자 캐스팅 모드가 변경될 경우 발행합니다.
/// </summary>
public readonly struct OnOptionShadowChanged
{
    public readonly ShadowCastingMode shadowCastingMode;

    public OnOptionShadowChanged(ShadowCastingMode shadowCastingMode) {
        this.shadowCastingMode = shadowCastingMode;
    }

    public static void Publish(ShadowCastingMode shadowCastingMode)
    {
        CEventBus<OnOptionShadowChanged>.Publish(new OnOptionShadowChanged(shadowCastingMode));
    }
}
