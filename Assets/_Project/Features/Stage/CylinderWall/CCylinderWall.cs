using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CCylinderWall : AFrameable, IFixedUpdateFrameable
{
    [SerializeField] private float _offset=1f;
    [SerializeField] private Image _fogImage;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CapsuleCollider _col;
    private GameObject _player;
    private Rigidbody _rb;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteFixedUpdateFrame()
    {
        Vector3 center = transform.position;
        Vector3 playerPos = _player.transform.position;

        Vector3 dir = playerPos - center;
        dir.y = 0;

        Vector3 playerforward=_player.transform.forward;
        playerforward.y = 0;
        
        //안개를 바라볼수록 각도가 작아짐.
        float fogAngle = Vector3.Angle(dir, playerforward);


        float distance = dir.magnitude;
        float radius = _col.radius * transform.localScale.x;
        distance = Mathf.Clamp(distance, radius - _offset, radius);


        Color tmp = _fogImage.color;
        //거리에 따른 불투명도 * 각도에 따른 불투명도
        tmp.a = Mathf.Clamp((1 - ((radius - distance) / _offset)), 0, 0.8f)*(1.0f-fogAngle/180.0f);
        _fogImage.color = tmp;
        if (distance >=radius)
        {
            Vector3 boundaryPos = center + dir.normalized * radius;
            boundaryPos.y = _player.transform.position.y;

            _rb.position = boundaryPos;

            Vector3 normal = dir.normalized;
            float normalVelocity = Vector3.Dot(_rb.velocity, normal);

            if (normalVelocity > 0)
            {
                _rb.velocity -= normal * normalVelocity;
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _col = GetComponent<CapsuleCollider>();
        _player = CNewGrab.Instance.gameObject;
        _rb = _player.GetComponent<Rigidbody>();
    }
    private void OnTriggerStay(Collider other)
    {
        
    }
    #endregion
}
