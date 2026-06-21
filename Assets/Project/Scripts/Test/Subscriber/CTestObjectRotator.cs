using UnityEngine;

/// <summary>
/// 오브젝트를 마우스에 따라 회전시키는 간단한 테스트 스크립트입니다.
/// </summary>
public class CTestObjectRotator : AMono
{
    [Header("회전 설정")]
    [SerializeField] private float _rotateSpeed = 0.5f;

    private float _rotationX;
    private float _rotationY;

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void LookHandler(OnInputLook ctx)
    {
        // 델타값 누적
        float mouseX = ctx.delta.x * _rotateSpeed;
        float mouseY = ctx.delta.y * _rotateSpeed;

        _rotationX += mouseX;
        _rotationY -= mouseY;
        _rotationX = Mathf.Clamp(_rotationX, -89f, 89f); // 위아래 회전 제한
        // 회전 적용
        transform.localRotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnInputLook>.Subscribe(LookHandler);
    }
    private void OnDisable()
    {
        CEventBus<OnInputLook>.Unsubscribe(LookHandler);
    }
    #endregion
}
