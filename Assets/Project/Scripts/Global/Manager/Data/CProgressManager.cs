using UnityEngine;

/// <summary>
/// 영속 진행도를 보유하고 디스크에 저장하는 매니저입니다.
/// </summary>
public sealed class CProgressManager : ASingleton<CProgressManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const string FILE_NAME = "progress"; // 저장 파일명
    private ProgressData _progress;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    /// <summary>현재 진행도 데이터를 읽습니다.</summary>
    public ProgressData Progress => _progress;

    #region 재화
    /// <summary>현재 보유 골드입니다.</summary>
    public int Money => _progress.money;

    /// <summary>골드를 추가하고 저장 및 변경 이벤트를 발행합니다.</summary>
    public void AddMoney(int amount)
    {
        _progress.money = Mathf.Max(0, _progress.money + amount);
        Save();
        OnMoneyChanged.Publish(_progress.money);
    }

    /// <summary>골드가 충분하면 차감하고 True를 반환합니다.</summary>
    /// <param name="cost">차감할 비용</param>
    public bool TrySpendMoney(int cost)
    {
        if (cost < 0 || _progress.money < cost) return false;

        _progress.money -= cost;
        Save();
        OnMoneyChanged.Publish(_progress.money);
        return true;
    }
    #endregion

    #region 업그레이드
    /// <summary>지정한 장비 타입의 현재 레벨을 반환합니다.</summary>
    /// <param name="gearType">장비 타입</param>
    public int GetGearLevel(EDataType gearType)
    {
        switch (gearType)
        {
            case EDataType.OxygenTank: return _progress.oxygenTankLevel;
            case EDataType.Radar: return _progress.radarLevel;
            case EDataType.Thruster: return _progress.thrusterLevel;
            case EDataType.GrabTool: return _progress.grabToolLevel;
            case EDataType.Bag: return _progress.bagLevel;
            default:
                UDebug.Print($"업그레이드 레벨 조회: 처리되지 않은 장비 타입({gearType})입니다.", LogType.Error);
                return 1;
        }
    }

    /// <summary>지정한 장비의 레벨을 1 올립니다. 이미 최대 레벨이면 실패합니다.</summary>
    /// <param name="gearType">장비 타입</param>
    /// <returns>레벨업 성공 여부</returns>
    public bool UpgradeGear(EDataType gearType)
    {
        // 최대 레벨 방어: 현재 레벨이 장비 SO의 MaxLevel 이상이면 올리지 않음
        AGearSO gear = GetGearSO(gearType);
        if (gear == null) return false;
        if (GetGearLevel(gearType) >= gear.MaxLevel)
        {
            UDebug.Print($"업그레이드: {gearType}이(가) 이미 최대 레벨입니다.", LogType.Warning);
            return false;
        }

        switch (gearType)
        {
            case EDataType.OxygenTank: ++_progress.oxygenTankLevel; break;
            case EDataType.Radar: ++_progress.radarLevel; break;
            case EDataType.Thruster: ++_progress.thrusterLevel; break;
            case EDataType.GrabTool: ++_progress.grabToolLevel; break;
            case EDataType.Bag: ++_progress.bagLevel; break;
            default:
                UDebug.Print($"업그레이드: 처리되지 않은 장비 타입({gearType})입니다.", LogType.Warning);
                return false;
        }

        Save();
        OnGearUpgraded.Publish(gearType, GetGearLevel(gearType));
        return true;
    }

    /// <summary>장비 타입에 해당하는 장비 SO를 반환합니다. (없으면 null)</summary>
    /// <param name="gearType">장비 타입</param>
    public AGearSO GetGearSO(EDataType gearType)
    {
        switch (gearType)
        {
            case EDataType.OxygenTank: return UData.OxygenTank();
            case EDataType.Radar: return UData.Radar();
            case EDataType.Thruster: return UData.Thruster();
            case EDataType.GrabTool: return UData.GrabTool();
            case EDataType.Bag: return UData.Bag();
            default:
                UDebug.Print($"장비 SO 조회: 처리되지 않은 장비 타입({gearType})입니다.", LogType.Error);
                return null;
        }
    }
    #endregion

    #region 진행 상황
    /// <summary>잠수함 하강으로 도달한 최대 스테이지입니다.</summary>
    public int UnlockedStage => _progress.unlockedStage;

    /// <summary>스테이지를 한 단계 더 깊게 갱신합니다.</summary>
    public void DescendStage()
    {
        ++_progress.unlockedStage;
        Save();
    }
    #endregion

    /// <summary>현재 진행도를 로컬 파일에 저장합니다.</summary>
    public void Save()
    {
        USaveFile.Save(FILE_NAME, _progress);
    }

    /// <summary>로컬 파일에서 진행도를 다시 불러옵니다.</summary>
    public void Load()
    {
        _progress = USaveFile.Load(FILE_NAME, new ProgressData());
    }

    /// <summary>진행도를 초기화하고 저장 파일을 삭제합니다.</summary>
    public void ResetProgress()
    {
        _progress = new ProgressData();
        USaveFile.Delete(FILE_NAME);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {
        _progress = USaveFile.Load(FILE_NAME, new ProgressData());
    }
    #endregion
}
