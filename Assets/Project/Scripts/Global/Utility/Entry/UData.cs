/// <summary>
/// UDatabaseManager의 진입점 역할을 하는 유틸리티 클래스입니다.
/// </summary>
public static class UData
{
    public static CDatabaseManager Address => CDatabaseManager.Ins;

    /// <summary>
    /// 문자열 ID로 SO를 반환받습니다.
    /// </summary>
    /// <typeparam name="T">가져올 SO 타입</typeparam>
    /// <param name="id">정적 SO ID</param>
    /// <returns></returns>
    public static T Get<T>(string id) where T : ABaseSO
        => Address.Get<T>(id);

    /// <summary>
    /// 문자열 ID로 ABaseSO를 반환받습니다.
    /// 구체적인 타입을 반환받으려면 제네릭을 사용해야 합니다.
    /// </summary>
    /// <param name="id">정적 SO ID</param>
    /// <returns></returns>
    public static ABaseSO Get(string id)
    => UData.Get<ABaseSO>(id);

    /// <summary>
    /// 문자열 ID로 수집품 SO를 반환받습니다.
    /// </summary>
    /// <param name="id">수집품 ID</param>
    /// <returns></returns>
    public static CCollectibleSO Collectible(string id)
        => Address.Get<CCollectibleSO>(id);

    /// <summary>
    /// 문자열 ID로 적 SO를 반환받습니다.
    /// </summary>
    /// <param name="id">적 ID</param>
    /// <returns></returns>
    public static CEnemySO Enemy(string id)
        => Address.Get<CEnemySO>(id);

    /// <summary>
    /// 문자열 ID로 잡기 도구 SO를 반환받습니다.
    /// </summary>
    public static CGrabToolSO GrabTool()
        => Address.Get<CGrabToolSO>(Id.GrabTool);

    /// <summary>
    /// 문자열 ID로 산소 SO를 반환받습니다.
    /// </summary>
    public static COxygenTankSO OxygenTank()
        => Address.Get<COxygenTankSO>(Id.OxygenTank);

    /// <summary>
    /// 문자열 ID로 레이더 SO를 반환받습니다.
    /// </summary>
    public static CRadarSO Radar()
        => Address.Get<CRadarSO>(Id.Rader);

    /// <summary>
    /// 문자열 ID로 추진기 SO를 반환받습니다.
    /// </summary>
    public static CThrusterSO Thruster()
        => Address.Get<CThrusterSO>(Id.Thruster);

    /// <summary>
    /// 문자열 ID로 가방 SO를 반환받습니다.
    /// </summary>
    public static CBagSO Bag()
        => Address.Get<CBagSO>(Id.Bag);

    /// <summary>
    /// 오디오 ID로 사운드 SO를 반환받습니다.
    /// </summary>
    /// <param name="id">오디오 ID</param>
    /// <returns></returns>
    public static CSoundSO Sound(string id)
        => Address.Get<CSoundSO>(id);
}
