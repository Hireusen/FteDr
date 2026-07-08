using System.Collections;
using UnityEngine;

/// <summary>
/// 부트 씬 전용. 부트 시퀀스가 끝나면 화면을 어둡게 덮으며 타이틀 씬으로 넘어갑니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class CBootToTitle : AMono
{
    private IEnumerator CoLoad()
    {
        while (!CBootManager.IsInitialized)
        {
            yield return null;
        }
        UScene.Load(EScene.Title);
    }

    private void Start()
    {
        StartCoroutine(CoLoad());
    }
}
