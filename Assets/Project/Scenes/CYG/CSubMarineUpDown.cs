using System.Collections;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CSubMarineUpDown : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    bool _moveOn = false;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    //type=-1 하강, type=1 상승
    public void MoveSubmarine(int type)
    {
        if (_moveOn == true) return;
        _moveOn = true;
        UFade.FadeOut(1.5f, true);
        StartCoroutine(MoveSubmarineCo(type,3f));
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator MoveSubmarineCo(int movetype,float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {

            switch (movetype)
            {
                case -1:
                    gameObject.transform.position += Vector3.up * -10 * Time.deltaTime;
                    break;
                case 1:
                    gameObject.transform.position += Vector3.up * +10 * Time.deltaTime;
                    break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        _moveOn = false;
        
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
