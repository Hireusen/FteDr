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
    public HashSet<CrashInfo> CrashObjects { get;private set; }=new HashSet<CrashInfo>();

    public void RemoveCrashinfo(CrashInfo crashInfo)
    {
        if (CrashObjects.Contains(crashInfo))
        {
            CrashObjects.Remove(crashInfo);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        //objects에 넣은걸 교체하기 위한, crashobjects 코드 추가
        if (collision.gameObject.CompareTag(K.TAG_GRABABLE))
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
            crashobject.crashedObject = collision.transform.root.gameObject;
            crashobject.crashPoint = avgPoint;

            CrashObjects.Add(crashobject);
            print(_testnum + "attach");
        }
        
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(K.TAG_GRABABLE))
        {
            GameObject crashedobj = collision.transform.root.gameObject;
            if (Objects.Contains(crashedobj))
            {
                Objects.Remove(crashedobj);
                print(_testnum + "deattach");
            }
            CrashInfo temp=new CrashInfo();
            temp.crashedObject = crashedobj;
            if (CrashObjects.Contains(temp))
            {
                CrashObjects.Remove(temp);
            }
            
        }
    }
    private void OnTriggerEnter(Collider collision)
    {
        //objects에 넣은걸 교체하기 위한, crashobjects 코드 추가
        CrashInfo temp = new CrashInfo();
        temp.crashedObject = collision.transform.root.gameObject;
        if (temp.crashedObject.CompareTag(K.TAG_GRABABLE))
        //if (collision.gameObject.CompareTag(K.TAG_GRABABLE))
        {
            Objects.Add(collision.gameObject);

            Collider myCollider = GetComponent<Collider>();
            Vector3 contactPoint = collision.ClosestPoint(myCollider.bounds.center);
            CrashInfo crashobject = new CrashInfo();
            crashobject.crashedObject = temp.crashedObject;
            crashobject.crashPoint = contactPoint;

            CrashObjects.Add(crashobject);
            print(_testnum + "attach");

        }
    }
    private void OnTriggerExit(Collider collision)
    {
        CrashInfo temp = new CrashInfo();
        temp.crashedObject = collision.transform.root.gameObject;
        if (temp.crashedObject.CompareTag(K.TAG_GRABABLE))
        //if (collision.gameObject.CompareTag(K.TAG_GRABABLE))
        {
            if (Objects.Contains(collision.gameObject))
            {
                Objects.Remove(collision.gameObject);
                print(_testnum + "deattach");
            }
           // temp.crashedObject = collision.gameObject;
            if (CrashObjects.Contains(temp))
            {
                CrashObjects.Remove(temp);
            }

        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
