using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CWall : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField]private float _currentSpeed = 5f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            print("push");
            Vector3 dir = -collision.contacts[0].normal;
           // collision.gameObject.GetComponent<Rigidbody>().AddForce(dir * _pushForce, ForceMode.Impulse);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 playerVelocity = playerRb.velocity;
                Vector3 streamDirection = transform.right; // 질문 주신 스크립트의 방향 유지

                // 플레이어가 해류 반대 방향으로 진입하려는 속도 측정
                float opposingSpeed = Vector3.Dot(playerVelocity, -streamDirection);

                if (opposingSpeed > 0)
                {
                    print("push");

                    // [수정] 들어오려는 속도(opposingSpeed)에 밀어내는 배율(_currentSpeed)을 곱함
                    // ForceMode.VelocityChange를 사용해 질량을 무시하고 즉각 속도를 변경
                    Vector3 resistance = streamDirection * (opposingSpeed * _currentSpeed);

                    playerRb.AddForce(resistance, ForceMode.VelocityChange);
                }
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
