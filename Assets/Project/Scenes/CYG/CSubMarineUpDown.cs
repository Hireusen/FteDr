using Cinemachine;
using System.Collections;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CSubMarineUpDown : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    [SerializeField] CinemachineVirtualCamera controlCam;
    [SerializeField] int firstSceneIndex = 3;
    [SerializeField] int lastSceneIndex = 4;
    private float accelalation = 5f;
    private const string DESTNAME = "Destination";
    private const string UPPOS = "UpPos";
    private const string DOWNPOS = "Downpos";
    private const string ARRIVECAM = "ArriveCamera";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    bool _moveOn = false;
    CinemachineVirtualCamera arriveCam;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public static CSubMarineUpDown Instance;
    //movesubmarine함수 호출하기 전에 해당 함수로 가능한건지 먼저 확인할것
    //type=-1 아래로 갈 수 있는지 확인
    //type=1 위로 갈 수 있는지 확인
    public bool CanMove(int type)
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex - type;
        if (nextSceneIndex >= firstSceneIndex && nextSceneIndex <= lastSceneIndex) return true;

        return false;


    }
    //type=-1 하강, type=1 상승
    public void MoveSubmarine(int type)
    {
        if (_moveOn == true) return;
        _moveOn = true;
        StartCoroutine(MoveSubmarineSlowStartCo(type,1.5f));
        UFade.FadeOut(1.5f, true);

        if(type==-1)
            UScene.NextLoad(delay:2f,onComplete: ()=>ArriveSubmarine(3f, type));
        else
            UScene.PrevLoad(delay: 2f, onComplete: () => ArriveSubmarine(3f, type));


    }

    //type=-1 이면 아래로 내려가는 동작. type=1이면 올라가는 동작
    public void ArriveSubmarine(float duration,int type=-1)
    {
        UFade.FadeIn(2f, true);
        print(_moveOn);
        
        if (_moveOn == true) return;
        print("Arrive");
        _moveOn = true;
        arriveCam = GameObject.Find(ARRIVECAM).GetComponent<CinemachineVirtualCamera>();
        arriveCam.LookAt = gameObject.transform;
        arriveCam.Priority = 20;
        controlCam.Priority = 10;
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
        print("moveon->false");
        
    }
    
    private IEnumerator MoveStartToDestCo(Vector3 startPos, Vector3 destPos,float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            t = 1f - (1f - t) * (1f - t);

            transform.position = Vector3.Lerp(startPos, destPos, t);
            timer += Time.deltaTime;
            yield return null;
        }
        arriveCam.Priority = 10;
        controlCam.Priority = 20;
        _moveOn = false;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
