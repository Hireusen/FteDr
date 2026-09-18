using UnityEngine;

// 2D 사각형 오브젝트 회전시키기
public class SquareRotate : MonoBehaviour
{


    private float _a;
    private float _b;
    // 매 프레임 5도씩 회전
    private void Update()
    {
        _a = transform.right.z;
        _b = transform.eulerAngles.z;

        Quaternion rotation = Quaternion.AngleAxis(5f, Vector3.forward);
        transform.rotation *= rotation;
    }

    private void OnGUI()
    {
        GUILayout.Box($"{_a}, {_b}");
    }
}
