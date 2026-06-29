using UnityEngine;

/// <summary>
/// 사운드 재생의 진입점 역할을 하는 퍼사드 클래스입니다.
/// </summary>
public static class USound
{
    /// <summary>사운드 매니저의 전역 인스턴스에 접근합니다.</summary>
    private static CSoundManager Manager => CSoundManager.Ins;

    #region ─────────────────────────▶ SFX ◀─────────────────────────
    /// <summary>메인 카메라 위치에서 효과음을 재생합니다.</summary>
    /// <param name="id">사운드 ID (Id.Sfx_*)</param>
    public static void PlaySfx(string id) => Manager.PlaySfx(id);

    /// <summary>지정한 좌표에서 3D 효과음을 재생합니다. (SO 기본 거리)</summary>
    public static void PlaySfx(string id, Vector3 pos) => Manager.PlaySfx(id, pos);

    /// <summary>지정한 좌표에서 3D 효과음을 재생합니다. (거리 덮어쓰기)</summary>
    public static void PlaySfx(string id, Vector3 pos, float minDistance, float maxDistance)
        => Manager.PlaySfx(id, pos, minDistance, maxDistance);

    /// <summary>지정한 대상을 따라다니며 3D 효과음을 재생합니다. (SO 기본 거리)</summary>
    public static void PlaySfx(string id, Transform target) => Manager.PlaySfx(id, target);

    /// <summary>지정한 대상을 따라다니며 3D 효과음을 재생합니다. (거리 덮어쓰기)</summary>
    public static void PlaySfx(string id, Transform target, float minDistance, float maxDistance)
        => Manager.PlaySfx(id, target, minDistance, maxDistance);

    /// <summary>재생 중인 모든 3D 효과음을 즉시 중단합니다. (카메라 효과음 제외)</summary>
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
    /// <summary>환경음(바다·동굴 등)을 설정하고 재생합니다. BGM과 동시 재생됩니다.</summary>
    public static void PlayAmbience(string id) => Manager.PlayAmbience(id);

    /// <summary>환경음을 중단합니다.</summary>
    public static void StopAmbience() => Manager.StopAmbience();

    /// <summary>환경음 재생 여부를 반환합니다.</summary>
    public static bool IsPlayingAmbience() => Manager.IsPlayingAmbience();
    #endregion

    #region ─────────────────────────▶ 수중 / 볼륨 ◀─────────────────────────
    /// <summary>수중 분위기(로우패스)를 전역으로 켜거나 끕니다. 이후 재생되는 3D 효과음에 적용됩니다.</summary>
    public static void SetUnderwater(bool enabled, float cutoffHz = 1500f)
        => Manager.SetUnderwater(enabled, cutoffHz);

    /// <summary>볼륨 설정 변경 시 현재 재생 중인 BGM/Ambience에 즉시 반영합니다.</summary>
    public static void RefreshVolume() => Manager.RefreshVolume();
    #endregion
}
