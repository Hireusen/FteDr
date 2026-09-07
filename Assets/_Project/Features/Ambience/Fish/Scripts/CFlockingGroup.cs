using UnityEngine;

/// <summary>
/// 개별 물고기 군집의 설정(스폰 위치, 타겟, 범위)을 보관하는 순수 데이터 컨테이너입니다.
/// </summary>
public sealed class CFlockingGroup : MonoBehaviour
{
    [Header("물고기 테이블 프리셋")]
    public CFishPresetSO preset;

    [Header("군집 설정")]
    [Min(1)] public int numFish = 20;
    [Min(0.1f)] public float averageSpeed = 2f;
    [Range(0.5f, 12f)] public float turnSpeed = 3f;
    public float spreadRadius = 3f;

    [Header("이동 범위 및 타겟")]
    [SerializeField] private Vector3 _boundsMin = new Vector3(-200f, 48f, -200f);
    [SerializeField] private Vector3 _boundsMax = new Vector3(200f, 100f, 200f);
    [SerializeField] private Transform _target;

    public Vector3 BoundsMin => _boundsMin;
    public Vector3 BoundsMax => _boundsMax;
    public Transform Target => _target;
}
