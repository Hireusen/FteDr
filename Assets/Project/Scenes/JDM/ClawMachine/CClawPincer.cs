using UnityEngine;

/// <summary>
/// [4단계] 집게 발톱(양옆 손가락)의 개폐를 담당합니다. (ClawHead에 부착)
/// 두 손가락을 서로 반대 방향으로 회전시켜 열고 닫습니다. (팀원 기존 모델 방식과 호환)
///
/// Shooter가 상태에 따라 Open()/Close()를 지시하고, 이 컴포넌트는 회전만 담당합니다.
/// 회전 축은 인스펙터에서 조정 가능(기본 Y). 열림/닫힘 각도도 조정 가능.
/// </summary>
public sealed class CClawPincer : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("손가락 참조")]
    [Tooltip("왼쪽 손가락")]
    [SerializeField] private Transform _fingerL;
    [Tooltip("오른쪽 손가락")]
    [SerializeField] private Transform _fingerR;

    [Header("회전 설정")]
    [Tooltip("회전 축 (기본 Y)")]
    [SerializeField] private EAxis _axis = EAxis.Y;
    [Tooltip("완전히 열렸을 때 각도(도)")]
    [SerializeField] private float _openAngle = 30f;
    [Tooltip("완전히 닫혔을 때(빈손) 각도(도)")]
    [SerializeField] private float _closeAngle = 0f;
    [Tooltip("여닫는 속도(도/초)")]
    [SerializeField] private float _speed = 360f;

    [Tooltip("발톱 회전 중심에서 발톱 끝까지 길이 (크기 대응 각도 계산용)")]
    [SerializeField] private float _fingerLength = 1f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _currentAngle;   // 현재 벌어진 각도
    private float _targetAngle;    // 목표 각도
    private Quaternion _baseL;     // 손가락 초기 로컬 회전 (기준)
    private Quaternion _baseR;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 목표만큼 닫혀서 거의 도달했는지 여부입니다.</summary>
    public bool IsSettled => Mathf.Abs(_currentAngle - _targetAngle) < 1f;

    /// <summary>발톱을 완전히 엽니다. (평상시)</summary>
    public void Open()
    {
        _targetAngle = _openAngle;
    }

    /// <summary>발톱을 완전히 닫습니다. (빈손 잡기)</summary>
    public void Close()
    {
        _targetAngle = _closeAngle;
    }

    /// <summary>
    /// 물체 반지름에 맞춰 표면에 닿을 만큼만 닫습니다.
    /// 발톱 길이와 물체 반지름의 기하 관계로 목표 각도를 역산합니다.
    /// </summary>
    /// <param name="objectRadius">잡을 물체의 반지름(월드 단위)</param>
    public void CloseOnObject(float objectRadius)
    {
        // 발톱이 벌어진 각도 θ일 때 두 발톱 끝 간격 ≈ 2 * fingerLength * sin(θ).
        // 물체 지름(2r)에 맞추려면 sin(θ) = r / fingerLength.
        float ratio = Mathf.Clamp01(objectRadius / Mathf.Max(0.0001f, _fingerLength));
        float angle = Mathf.Asin(ratio) * Mathf.Rad2Deg;

        // 완전 닫힘~완전 열림 범위로 제한 (물체가 발톱보다 크면 최대한 벌린 채 멈춤)
        _targetAngle = Mathf.Clamp(angle, _closeAngle, _openAngle);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // 손가락의 초기 로컬 회전을 기준으로 저장 (여기서 ±각도를 더한다)
        if (_fingerL != null) _baseL = _fingerL.localRotation;
        if (_fingerR != null) _baseR = _fingerR.localRotation;

        _currentAngle = _openAngle; // 평상시 열린 상태로 시작
        _targetAngle = _currentAngle;
        ApplyRotation();
    }

    private void Update()
    {
        if (Mathf.Approximately(_currentAngle, _targetAngle)) return;

        _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, _speed * Time.deltaTime);
        ApplyRotation();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 현재 각도를 두 손가락에 서로 반대로 적용한다.
    private void ApplyRotation()
    {
        Vector3 axis = AxisVector();
        if (_fingerL != null) _fingerL.localRotation = _baseL * Quaternion.AngleAxis(_currentAngle, axis);
        if (_fingerR != null) _fingerR.localRotation = _baseR * Quaternion.AngleAxis(-_currentAngle, axis);
    }

    private Vector3 AxisVector()
    {
        switch (_axis)
        {
            case EAxis.X:
                return Vector3.right;
            case EAxis.Z:
                return Vector3.forward;
            case EAxis.Y:
            default:
                return Vector3.up;
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    public enum EAxis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }
    #endregion
}
