using UnityEngine;

/// <summary>
/// 카메라 조회 기능을 제공하는 정적 유틸리티입니다.
/// </summary>
public static class UCamera
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private static Camera _camera; // 캐싱된 활성 카메라 (시네머신 브레인이 붙은 실제 카메라)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 현재 활성 카메라입니다.
    /// 명시적으로 주입된 값이 있으면 그것을, 없으면 Camera.main을 찾아 캐싱합니다.
    /// </summary>
    public static Camera Main
    {
        get
        {
            if (_camera == null) _camera = Camera.main;
            return _camera;
        }
    }

    /// <summary>활성 카메라가 존재하는지 여부입니다.</summary>
    public static bool HasCamera => Main != null;

    /// <summary>
    /// 카메라를 명시적으로 지정합니다.
    /// 씬 전환 등으로 카메라가 교체될 때, 캐싱된 참조를 갱신하기 위해 호출합니다.
    /// null을 넣으면 다음 조회 때 Camera.main으로 다시 탐색합니다.
    /// </summary>
    /// <param name="camera">활성 카메라</param>
    public static void SetCamera(Camera camera)
    {
        _camera = camera;
    }
    #endregion

    #region ─────────────────────────▶ 조회 ◀─────────────────────────
    /// <summary>월드 좌표를 스크린 좌표로 변환합니다. (카메라 없으면 zero)</summary>
    public static Vector3 WorldToScreen(Vector3 worldPos)
    {
        Camera cam = Main;
        return cam != null ? cam.WorldToScreenPoint(worldPos) : Vector3.zero;
    }

    /// <summary>해당 월드 좌표가 카메라 시야(프러스텀) 안에 있는지 검사합니다. (카메라 없으면 false)</summary>
    public static bool IsInView(Vector3 worldPos)
    {
        Camera cam = Main;
        if (cam == null) return false;

        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }
    #endregion
}
