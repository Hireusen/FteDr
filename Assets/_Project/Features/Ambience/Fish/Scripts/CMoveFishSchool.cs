using UnityEngine;

/// <summary>
/// 물고기 군집의 타겟 오브젝트를 불규칙하게 원격 이동시키는 제어 클래스입니다.
/// </summary>
public sealed class CFlockingTargetMover : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이동 및 타이밍")]
    [SerializeField] private float _moveRange = 5f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private Vector2 _positionChangeSpeed = new Vector2(3f, 8f);
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _timer;
    private Vector3 _originalPosition;
    private Vector3 _targetPosition;
    private Transform _targetTransform;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Awake()
    {
        _originalPosition = transform.position;
        if (transform.childCount > 0)
        {
            _targetTransform = transform.GetChild(0);
            _targetPosition = _targetTransform.position;
        }
        else
        {
            UDebug.Print("자식으로 등록된 타겟 트랜스폼을 찾을 수 없습니다.", LogType.Error, this);
        }
    }

    /// <summary>
    /// 프레임 매니저에 의해 매 프레임 호출되는 로직입니다.
    /// </summary>
    public void ExecuteUpdateFrame()
    {
        if (_targetTransform == null) return;

        if (_timer >= 0f)
        {
            _timer -= Time.deltaTime;
            _targetTransform.position = Vector3.MoveTowards(
                _targetTransform.position,
                _targetPosition,
                _moveSpeed * Time.deltaTime
            );
        }
        else
        {
            _timer = Random.Range(_positionChangeSpeed.x, _positionChangeSpeed.y);
            // 누적식 위치 변경
            _originalPosition += Random.insideUnitSphere * _moveRange;
            _targetPosition = _originalPosition;
        }
    }
    #endregion
}
