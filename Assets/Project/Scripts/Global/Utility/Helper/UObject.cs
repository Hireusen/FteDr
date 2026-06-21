using UnityEngine;

/// <summary>
/// 게임 오브젝트를 다루는 유틸리티 클래스입니다.
/// </summary>
public static class UObject
{
    #region ─────────────────────────▶ 게임 오브젝트 ◀─────────────────────────
    /// <summary>
    /// 빈 게임 오브젝트를 생성합니다.
    /// </summary>
    /// <param name="name">이름</param>
    /// <param name="parent">부모 오브젝트</param>
    /// <param name="worldPosStays">월드 좌표 유지 여부</param>
    public static GameObject Create
        (string name, Transform parent = null, bool worldPosStays = false)
    {
        GameObject go = new GameObject(name);
        if (parent != null) {
            go.transform.SetParent(parent, worldPosStays);
        }
        return go;
    }

    /// <summary>
    /// 특정 트랜스폼의 모든 자식을 파괴합니다.
    /// </summary>
    /// <param name="parent">트랜스폼</param>
    public static void DestroyChildren(Transform parent, float delay = 0)
    {
        if (parent == null) return;

        int length = parent.childCount;
        for (int i = length - 1; i >= 0; --i) {
            GameObject.Destroy(parent.GetChild(i).gameObject, delay);
        }
    }

    /// <summary>
    /// 게임 오브젝트를 활성화 또는 비활성화합니다.
    /// </summary>
    /// <param name="go">게임 오브젝트</param>
    /// <param name="isActive">활성화 여부</param>
    public static void SetActive(GameObject go, bool isActive)
    {
        if (go == null) return;
        if (go.activeSelf == isActive) return;

        go.SetActive(isActive);
    }

    /// <summary>
    /// 게임 오브젝트의 부모를 설정합니다.
    /// </summary>
    /// <param name="go">게임 오브젝트</param>
    /// <param name="parent">부모 트랜스폼</param>
    public static void SetParent(GameObject go, Transform parent, bool worldPosStays = false)
    {
        if (go == null) return;

        UObject.SetParent(go.transform, parent, worldPosStays);
    }

    /// <summary>
    /// 트랜스폼의 부모를 설정합니다.
    /// </summary>
    /// <param name="tr">트랜스폼</param>
    /// <param name="parent">부모 트랜스폼</param>
    public static void SetParent(Transform tr, Transform parent, bool worldPosStays = false)
    {
        if (tr == null) return;

        tr.SetParent(parent, worldPosStays);
    }

    /// <summary>
    /// 트랜스폼의 좌표, 회전, 크기를 초기화합니다.
    /// </summary>
    /// <param name="tr">트랜스폼</param>
    public static void ResetTransform(Transform tr)
    {
        if (tr == null) return;

        tr.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        tr.localScale = Vector3.one;
    }
    #endregion

    #region ─────────────────────────▶ 컴포넌트 ◀─────────────────────────
    /// <summary>
    /// 씬에서 특정 게임 오브젝트의 특정 컴포넌트를 찾습니다.
    /// True → 비활성화된 오브젝트도 검색
    /// False → 활성화된 오브젝트만 검색
    /// </summary>
    /// <returns>컴포넌트</returns>
    public static T FindComponent<T>(bool inactive = true) where T : Component
    {
        return Object.FindAnyObjectByType<T>
            (inactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
    }

    /// <summary>
    /// 씬에서 특정 게임 오브젝트의 특정 컴포넌트를 모두 찾습니다.
    /// True → 비활성화된 오브젝트도 검색
    /// False → 활성화된 오브젝트만 검색
    /// </summary>
    /// <returns>컴포넌트</returns>
    public static T[] FindComponents<T>
        (bool inactive = false, FindObjectsSortMode sortMode = FindObjectsSortMode.None) where T : Component
    {
        return Object.FindObjectsByType<T>
            (inactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, sortMode);
    }
    #endregion
}
