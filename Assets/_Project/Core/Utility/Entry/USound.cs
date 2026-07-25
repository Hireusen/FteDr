using UnityEngine;

/// <summary>
/// 사운드 재생의 진입점 역할을 하는 퍼사드 클래스입니다.
/// </summary>
public static class USound
{
    /// <summary>사운드 매니저의 전역 인스턴스에 접근합니다.</summary>
    private static CSoundManager Manager => CSoundManager.Ins;

    #region ─────────────────────────▶ SFX ◀─────────────────────────
    /// <summary>메인 카메라 위치에서 효과음을 재생하고, 제어용 이미터를 반환합니다.</summary>
    /// <param name="id">사운드 ID (Id.Sfx_*)</param>
    /// <returns>재생 이미터. 개별 중단 시 StopImmediate()/FadeOutAndReturn(). 재생 실패 시 null.</returns>
    /// <remarks>
    /// 반환된 이미터는 재생 종료 후 풀에 반납·재사용됩니다.
    /// 원샷 효과음의 핸들을 오래 보관했다가 뒤늦게 중단하면 다른 소리를 멈출 수 있으니,
    /// 수동 중단이 필요한 지속음(루프 등)에만 핸들을 보관하세요.
    /// </remarks>
    public static CSoundEmitter PlaySfx(string id) => Manager.PlaySfx(id);

    /// <summary>지정한 좌표에서 3D 효과음을 재생하고, 제어용 이미터를 반환합니다. (SO 기본 거리)</summary>
    /// <returns>재생 이미터. 재생 실패 시 null.</returns>
    public static CSoundEmitter PlaySfx(string id, Vector3 pos) => Manager.PlaySfx(id, pos);

    /// <summary>지정한 좌표에서 3D 효과음을 재생하고, 제어용 이미터를 반환합니다. (거리 덮어쓰기)</summary>
    /// <returns>재생 이미터. 재생 실패 시 null.</returns>
    public static CSoundEmitter PlaySfx(string id, Vector3 pos, float minDistance, float maxDistance)
        => Manager.PlaySfx(id, pos, minDistance, maxDistance);

    /// <summary>지정한 대상을 따라다니며 3D 효과음을 재생하고, 제어용 이미터를 반환합니다. (SO 기본 거리)</summary>
    /// <returns>재생 이미터. 재생 실패 시 null.</returns>
    public static CSoundEmitter PlaySfx(string id, Transform target) => Manager.PlaySfx(id, target);

    /// <summary>지정한 대상을 따라다니며 3D 효과음을 재생하고, 제어용 이미터를 반환합니다. (거리 덮어쓰기)</summary>
    /// <returns>재생 이미터. 재생 실패 시 null.</returns>
    public static CSoundEmitter PlaySfx(string id, Transform target, float minDistance, float maxDistance)
        => Manager.PlaySfx(id, target, minDistance, maxDistance);

    /// <summary>재생 중인 모든 3D 효과음을 즉시 중단합니다.</summary>
    public static void StopAllSfx() => Manager.StopAllSfx();

    /// <summary>재생 중인 모든 3D 효과음을 페이드 아웃한 뒤 중단합니다.</summary>
    /// <param name="duration">페이드 시간(초)</param>
    public static void StopAllSfx(float duration) => Manager.StopAllSfx(duration);
    #endregion

    #region ─────────────────────────▶ BGM ◀─────────────────────────
    /// <summary>배경음을 설정하고 재생합니다. 한 번에 하나만 재생됩니다.</summary>
    public static void PlayBgm(string id) => Manager.PlayBgm(id);

    /// <summary>배경음을 중단합니다.</summary>
    public static void StopBgm() => Manager.StopBgm();

    /// <summary>배경음 재생 여부를 반환합니다.</summary>
    public static bool IsPlayingBgm() => Manager.IsPlayingBgm();
    #endregion

    #region ─────────────────────────▶ Ambience ◀─────────────────────────
    /// <summary>환경음을 설정하고 재생합니다. BGM과 동시 재생됩니다.</summary>
    public static void PlayAmbience(string id) => Manager.PlayAmbience(id);

    /// <summary>환경음을 중단합니다.</summary>
    public static void StopAmbience() => Manager.StopAmbience();

    /// <summary>환경음 재생 여부를 반환합니다.</summary>
    public static bool IsPlayingAmbience() => Manager.IsPlayingAmbience();
    #endregion

    #region ─────────────────────────▶ 수중 / 로우패스 / 볼륨 ◀─────────────────────────
    /// <summary>
    /// 수중 분위기(로우패스)를 전역으로 켜거나 끕니다.
    /// 재생 중인 BGM·Ambience·SFX 전부에 즉시 반영되며, 이후 재생분에도 적용됩니다.
    /// </summary>
    public static void SetUnderwater(bool enabled, float cutoffHz = 750f)
        => Manager.SetUnderwater(enabled, cutoffHz);

    /// <summary>
    /// BGM 로우패스를 SO·전역과 무관하게 강제로 지정합니다. (오버라이드)
    /// 자동 규칙으로 되돌리려면 ClearBgmLowPassOverride를 호출하세요.
    /// </summary>
    public static void SetBgmLowPass(bool enabled, float cutoffHz = 750f)
        => Manager.SetBgmLowPass(enabled, cutoffHz);

    /// <summary>BGM 로우패스 오버라이드를 해제하고 SO·전역 자동 규칙으로 되돌립니다.</summary>
    public static void ClearBgmLowPassOverride() => Manager.ClearBgmLowPassOverride();

    /// <summary>
    /// 환경음 로우패스를 SO·전역과 무관하게 강제로 지정합니다. (오버라이드)
    /// 자동 규칙으로 되돌리려면 ClearAmbienceLowPassOverride를 호출하세요.
    /// </summary>
    public static void SetAmbienceLowPass(bool enabled, float cutoffHz = 750f)
        => Manager.SetAmbienceLowPass(enabled, cutoffHz);

    /// <summary>환경음 로우패스 오버라이드를 해제하고 SO·전역 자동 규칙으로 되돌립니다.</summary>
    public static void ClearAmbienceLowPassOverride() => Manager.ClearAmbienceLowPassOverride();

    /// <summary>볼륨 설정 변경 시 현재 재생 중인 BGM/Ambience/SFX에 즉시 반영합니다.</summary>
    public static void RefreshVolume() => Manager.RefreshVolume();

    /// <summary>
    /// 전역·개별 로우패스를 중첩할 때의 차단 주파수를 계산합니다.
    /// 켜진 축만 골라 더 낮은(더 먹먹한) 쪽을 채택합니다. 둘 다 꺼졌으면 22000Hz(사실상 무필터).
    /// CSoundEmitter·CSoundManager가 공유하는 순수 계산 헬퍼입니다.
    /// </summary>
    /// <param name="globalOn">전역 로우패스 활성 여부</param>
    /// <param name="globalCutoff">전역 차단 주파수(Hz)</param>
    /// <param name="soOn">개별(SO) 로우패스 활성 여부</param>
    /// <param name="soCutoff">개별(SO) 차단 주파수(Hz)</param>
    public static float ResolveCutoff(bool globalOn, float globalCutoff, bool soOn, float soCutoff)
    {
        if (globalOn && soOn) return Mathf.Min(globalCutoff, soCutoff);
        if (globalOn) return globalCutoff;
        if (soOn) return soCutoff;

        return 22000f; // 둘 다 꺼짐 → 필터 무효화 수준
    }
    #endregion
}
