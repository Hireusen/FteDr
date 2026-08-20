using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 런타임 데이터를 보유하고 스탯을 계산하는 매니저입니다.
/// </summary>
public sealed class CPlayerManager : ASingleton<CPlayerManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly PlayerRuntimeData _runtime = new();

    private EFuelState _fuelState = EFuelState.Normal;

    private readonly List<GameObject> _hiddenItems = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    /// <summary>플레이어 런타임 데이터를 읽습니다.</summary>
    public PlayerRuntimeData Runtime => _runtime;

    public EFuelState FuelState => _fuelState;

    /// <summary>
    /// 수집에 성공하여 비활성화된 수집품 오브젝트를 보관합니다.
    /// </summary>
    /// <param name="itemObj"></param>
    public void StoreHiddenItem(GameObject itemObj)
    {
        if (itemObj != null)
        {
            _hiddenItems.Add(itemObj);
        }
    }

    /// <summary>
    /// 상점에서 아이템을 판매할 때 호출하여, 가방에 들어있던 비활성화 수집품들을 메모리에서 완전히 파괴합니다.
    /// </summary>
    public void DestroyHiddenItems()
    {
        foreach (GameObject go in _hiddenItems)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        _hiddenItems.Clear();
    }
    #endregion

    #region ─────────────────────────▶ 연료 ◀─────────────────────────
    /// <summary>현재 연료량입니다.</summary>
    public float CurrentFuel => _runtime.currentFuel;

    /// <summary>현재 최대 연료량입니다.</summary>
    public float MaxFuel
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.FuelTank);
            return UData.FuelTank().MaxFuel(level);
        }
    }

    /// <summary>현재 연료가 경고 임계값 미만인지 여부입니다. (5% 미만 화면 이펙트 등)</summary>
    public bool IsFuelLow
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.FuelTank);
            float threshold = UData.FuelTank().WarningThreshold(level); // 0~1 비율
            float max = MaxFuel;
            if (max <= 0f) return true;

            return (_runtime.currentFuel / max) < threshold;
        }
    }

    /// <summary>새 잠수를 시작합니다. 연료를 최대로 채우고 페널티/소지품을 리셋합니다.</summary>
    public void ResetForNew()
    {
        _runtime.ResetForNew();
        _runtime.currentFuel = MaxFuel;
        PublishFuel();
        PublishBag();
        RefreshFuelState();
    }

    /// <summary>연료를 소모합니다.</summary>
    /// <param name="amount">소모량(양수)</param>
    public void ConsumeFuel(float amount)
    {
        _runtime.currentFuel = Mathf.Max(0f, _runtime.currentFuel - amount);
        PublishFuel();
        RefreshFuelState();
    }

    /// <summary>연료를 회복합니다.</summary>
    /// <param name="amount">회복량(양수)</param>
    public void RecoverFuel(float amount)
    {
        _runtime.currentFuel = Mathf.Min(MaxFuel, _runtime.currentFuel + amount);
        PublishFuel();
        RefreshFuelState();
    }

    /// <summary>피격 피해를 현재 연료량에 적용합니다. (절댓값 차감 후 비율 차감)</summary>
    /// <param name="flat">절댓값 피해량(양수)</param>
    /// <param name="ratio">비율 피해량(0~1, 예: 0.25 = 25%)</param>
    public void ApplyDamage(float flat, float ratio)
    {
        float fuel = Mathf.Max(0f, _runtime.currentFuel - Mathf.Max(0f, flat));
        fuel *= Mathf.Clamp01(1f - ratio);
        _runtime.currentFuel = Mathf.Max(0f, fuel);
        PublishFuel();
        RefreshFuelState();
    }
    #endregion

    #region ─────────────────────────▶ 가방 / 소지품 ◀─────────────────────────
    /// <summary>현재 가방 최대 슬롯 수입니다.</summary>
    public int BagCapacity
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.Bag);
            return UData.Bag().Capacity(level);
        }
    }

    /// <summary>
    /// 현재 가방에 담긴 아이템들의 총 무게(KG)입니다.
    /// </summary>
    public float MaxWeight
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.Bag);
            return UData.Bag().MaxWeight(level);
        }
    }

    /// <summary>
    /// 가방에 담긴 아이템들의 현재 총 무게(KG)입니다.
    /// </summary>
    public float CurrentWeight
    {
        get
        {
            float totalWeight = 0f;
            foreach (string collectibleId in _runtime.bagItems)
            {
                CCollectibleSO so = UData.Collectible(collectibleId);
                if (so != null)
                {
                    totalWeight += so.Weight;
                }
            }
            return totalWeight;
        }
    }

    /// <summary>가방에 빈 슬롯이 있는지 여부입니다.</summary>
    public bool HasBagSpace => _runtime.bagItems.Count < BagCapacity;

    /// <summary>지정한 수집품을 추가했을 때 무게 한도를 넘지 않는지 여부입니다.</summary>
    /// <param name="collectibleId">수집품 ID</param>
    public bool HasWeightSpace(string collectibleId)
    {
        CCollectibleSO so = UData.Collectible(collectibleId);
        float addedWeight = so != null ? so.Weight : 0f;
        return CurrentWeight + addedWeight <= MaxWeight;
    }

    /// <summary>일반 수집품을 가방에 담습니다. 공간이 없으면 false.</summary>
    /// <param name="collectibleId">수집품 ID</param>
    public bool TryAddToBag(string collectibleId)
    {
        if (!HasBagSpace) return false;
        if (!HasWeightSpace(collectibleId)) return false;

        _runtime.bagItems.Add(collectibleId);
        PublishBag();
        return true;
    }

    /// <summary>특수 수집품을 손에 듭니다. 이미 들고 있으면 False</summary>
    /// <param name="specialId">특수 수집품 ID</param>
    public bool TryHoldSpecial(string specialId)
    {
        if (!_runtime.heldSpecialItem.IsBlank()) return false;

        _runtime.heldSpecialItem = specialId;
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 사망 / 드롭 처리 ◀─────────────────────────
    public void DropAllBagItems(Vector3 dropCenter, CPlayerDropConfig config = null)
    {
        if (_runtime.bagItems.Count == 0) return;

        float radius = config != null ? config.ScatterRadius : 3f;
        float upForce = config != null ? config.ScatterUpForce : 5f;
        float outForce = config != null ? config.ScatterOutForce : 3f;

        foreach (string collectibleId in _runtime.bagItems)
        {
            CCollectibleSO so = UData.Collectible(collectibleId);
            if (so != null && so.Prefab != null)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 targetPos = dropCenter + new Vector3(randomCircle.x, 1f, randomCircle.y);

                Vector3 spawnPos = dropCenter + new Vector3(randomCircle.x * 0.2f, 1f, randomCircle.y * 0.2f);
                Quaternion randomRot = UnityEngine.Random.rotationUniform;

                GameObject go = Instantiate(so.Prefab, spawnPos, randomRot);
                go.transform.localScale = so.Prefab.transform.localScale * so.GetRandomScale();

                if (!so.IsAir)
                {
                    Rigidbody rb = go.GetOrAddComponent<Rigidbody>();
                    rb.mass = Mathf.Max(0.01f, so.Weight);
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    Vector3 forceDir = (targetPos - dropCenter).normalized;
                    forceDir.y = 0f;
                    rb.AddForce(forceDir * outForce + Vector3.up * upForce, ForceMode.Impulse);

                    StartCoroutine(SettleDroppedItemRoutine(go, rb));
                }
                else
                {
                    go.transform.position = targetPos;
                    if (go.TryGetComponent(out CCollectibleBob bob))
                    {
                        bob.Initialize();
                    }
                }
            }
        }

        foreach (GameObject hiddenObj in _hiddenItems)
        {
            if (hiddenObj != null) Destroy(hiddenObj);
        }

        // 3. 데이터 초기화
        _runtime.bagItems.Clear();
        _hiddenItems.Clear();
        PublishBag();
    }

    private IEnumerator SettleDroppedItemRoutine(GameObject go, Rigidbody rb)
    {
        float elapsed = 0f;
        float stillTime = 0f;
        float sleepSqr = 0.05f * 0.05f;

        while (go != null && rb != null)
        {
            if (rb.velocity.sqrMagnitude <= sleepSqr)
            {
                stillTime += Time.deltaTime;
                if (stillTime >= 0.4f)
                {
                    break;
                }
            }
            else
            {
                stillTime = 0f;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= 6f)
            {
                break;
            }

            yield return null;
        }

        if (rb != null)
        {
            Destroy(rb);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {

    }

    private void PublishFuel()
    {
        OnPlayerFuelChanged.Publish(_runtime.currentFuel, MaxFuel);
    }

    private void RefreshFuelState()
    {
        EFuelState next = _runtime.currentFuel <= 0 ? EFuelState.Depleted : IsFuelLow ? EFuelState.Low : EFuelState.Normal;

        if (next == _fuelState) return;

        EFuelState prev = _fuelState;
        _fuelState = next;
        OnPlayerFuelStateChanged.Publish(next, prev);
    }

    private void PublishBag()
    {
        OnPlayerBagChanged.Publish(_runtime.bagItems.Count, BagCapacity);
        OnPlayerWeightChanged.Publish(CurrentWeight, MaxWeight);
    }
    #endregion
}
