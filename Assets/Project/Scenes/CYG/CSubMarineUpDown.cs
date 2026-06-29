using Cinemachine;
using System.Collections;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CSubMarineUpDown : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    private float accelalation = 5f;
    private const string DESTNAME = "Destination";
    private const string UPPOS = "UpPos";
    private const string DOWNPOS = "Downpos";
    private const string ARRIVECAM = "ArriveCamera";
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
        StartCoroutine(MoveSubmarineSlowStartCo(type,1.5f));
        
    }

    //type=-1 이면 아래로 내려가는 동작. type=1이면 올라가는 동작
    public void ArriveSubmarine(float duration,int type=-1)
    {
        if (_moveOn == true) return;
        _moveOn = true;
        CinemachineVirtualCamera arriveCam = GameObject.Find(ARRIVECAM).GetComponent<CinemachineVirtualCamera>();
        arriveCam.LookAt = gameObject.transform;
        UFade.FadeIn(1.5f, true);
        GameObject dest = GameObject.Find(DESTNAME);
        GameObject downPos= GameObject.Find(DOWNPOS);
        GameObject upPos= GameObject.Find(UPPOS);
        if(type==-1)
            StartCoroutine(MoveStartToDestCo(upPos.transform.position, dest.transform.position, 3f));
        else
            StartCoroutine(MoveStartToDestCo(downPos.transform.position, dest.transform.position, 3f));
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator MoveSubmarineSlowStartCo(int movetype,float duration)
    {
        float timer = 0f;
        float speed = 0f;
        while (timer < duration)
        {
            speed=Mathf.MoveTowards(speed, 10f, accelalation* Time.deltaTime);
            switch (movetype)
            {
                case -1:
                    gameObject.transform.position += Vector3.up * -speed * Time.deltaTime;
                    break;
                case 1:
                    gameObject.transform.position += Vector3.up * speed* Time.deltaTime;
                    break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        _moveOn = false;
        
    }
    
    private IEnumerator MoveStartToDestCo(Vector3 startPos, Vector3 destPos,float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            t = 1f - (1f - t) * (1f - t);

            transform.position = Vector3.Lerp(startPos, destPos, t);
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
