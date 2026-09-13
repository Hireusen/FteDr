using System;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class QuaternionClass : MonoBehaviour
{
    [Header("회전량")]
    [SerializeField] private Vector3 _rotationDir = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Transform _enemyTr;
    [SerializeField] private float _degree = 45f;


    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    [ContextMenu("쿼터니언 Angle")]
    private void QuaternionAngle()
    {
        Quaternion a = Quaternion.Euler(90, 30, 90);
        Quaternion b = Quaternion.Euler(90, 90, 180);
        float dir = Quaternion.Angle(a, b); // 쿼터니언 2개 넣어서 벡터 기준으로 계산한 사이 각도를 반환
        Debug.Log($"{a}와 {b}의 Angle은 {dir}입니다.");
    }

    [ContextMenu("쿼터니언 Inverse")]
    private void QuaternionInverse()
    {
        if (transform == null)
        {
            UDebug.Print("없어요...", LogType.Error);
            return;
        }

        Quaternion rotation = Quaternion.Euler(_rotationDir);
        Quaternion inverseDir = Quaternion.Inverse(rotation);
        _rotationDir = inverseDir.eulerAngles;
    }

    [ContextMenu("쿼터니언 Inverse2")]
    private void QuaternionInverse2()
    {
        // 한 방향벡터에서 다른 방향벡터로 회전하는 쿼터니언을 생성하는 역할

    }
    #endregion

    private float GetDot(float y1, float y2)
    {
        Quaternion a = Quaternion.Euler(0f, y1, 0f);
        Quaternion b = Quaternion.Euler(0f, y2, 0f);
        return Quaternion.Dot(a, b);
    }

    [ContextMenu("Dot 테스트")]
    private void PrintDot()
    {
        UDebug.Print($"차이를 구했습니다. => {GetDot(-180f, 180f)}");
        UDebug.Print($"차이를 구했습니다. => {GetDot(0f, 180f)}");
        UDebug.Print($"차이를 구했습니다. => {GetDot(30f, 180f)}");
        UDebug.Print($"차이를 구했습니다. => {GetDot(45f, 180f)}");
        UDebug.Print($"차이를 구했습니다. => {GetDot(60f, 180f)}");
        UDebug.Print($"차이를 구했습니다. => {GetDot(90f, 180f)}");
        UDebug.Print($"차이를 구했습니다. => {GetDot(180f, 180f)}");


    }

    /*private Vector3 _saveDirection;
    private void Awake()
    {
        // 어떤 오브젝트를 바라보는 방향
        Vector3 player = transform.position;
        Vector3 enemy = _enemyTr.position;
        Vector3 distance = enemy - player;
        Vector3 direction = distance.normalized; // 플레이어가 적을 본다

        // X, Y
        float x = Mathf.Cos(90f);
        float y = Mathf.Sin(90f);




        // 실제 보기
        transform.rotation = Quaternion.LookRotation(direction);
    }*/

    private void Update()
    {
        const float MIN_DEGREE = -22.5f;
        const float MAX_DEGREE = 22.5f;
        float randomDir = UnityEngine.Random.Range(MIN_DEGREE, MAX_DEGREE);

        /*Vector2 resultDir = Quaternion.Euler(0f, 0f, randomDir) * transform.right;*/

        float radian = (transform.eulerAngles.z + randomDir) * Mathf.Deg2Rad;
        Vector2 resultDir = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));

        /*Vector2 playerDir = transform.right;
        float currentDir = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg; // 라디안을 구했다
        float sumDir = currentDir + randomDir; // 다시 디그리로 바꿧다

        float radian = sumDir * Mathf.Deg2Rad; // 라디안으로 바꿧다
        float x = Mathf.Cos(radian);
        float y = Mathf.Sin(radian);

        Vector2 resultDir = new Vector2(x, y); // 디그리로 바꿧다*/

        Debug.DrawRay(transform.position, resultDir * 10f, Color.red, Time.deltaTime * 3f);
    }
}
