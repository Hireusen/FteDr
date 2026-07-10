using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CFinger : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    [SerializeField] private int _testnum = 0;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public struct CrashInfo:IEquatable<CrashInfo>
    {
        public GameObject crashedObject;
        public Vector3 crashPoint;

        public bool Equals(CrashInfo other)
        {
            return crashedObject == other.crashedObject;
        }
        public override int GetHashCode()
        {
            return crashedObject != null ? crashedObject.GetHashCode() : 0;

        }
    }

    //objects -> crashobject 교체 시도
    public HashSet<GameObject>Objects  { get; private set; }=new HashSet<GameObject>();
    public HashSet<CrashInfo> crashObjects { get;private set; }=new HashSet<CrashInfo>();
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        //objects에 넣은걸 교체하기 위한, crashobjects 코드 추가
        if (collision.gameObject.CompareTag("Collectible"))
        {
            Objects.Add(collision.gameObject);


            Vector3 sumPoint = Vector3.zero;
            int contactCount = 0;
            foreach (ContactPoint contact in collision.contacts)
            {
                contactCount++;
                sumPoint += contact.point;
            }
            Vector3 avgPoint = sumPoint / contactCount;
            CrashInfo crashobject= new CrashInfo();
            crashobject.crashedObject = collision.gameObject;
            crashobject.crashPoint = avgPoint;

            crashObjects.Add(crashobject);
            print(_testnum + "attach");
        }
        
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Collectible"))
        {
            if (Objects.Contains(collision.gameObject))
            {
                Objects.Remove(collision.gameObject);
                print(_testnum + "deattach");
            }
            CrashInfo temp=new CrashInfo();
            temp.crashedObject = collision.gameObject;
            if (crashObjects.Contains(temp))
            {
                crashObjects.Remove(temp);
            }
            
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
