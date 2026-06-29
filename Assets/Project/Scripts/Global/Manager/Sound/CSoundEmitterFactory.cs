using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSoundEmitter의 생성 및 재사용을 담당하는 팩토리입니다.
/// </summary>
public sealed class CSoundEmitterFactory
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<CSoundEmitter> _idle = new();   // 반납되어 대기 중인 이미터
    private readonly List<CSoundEmitter> _active = new();  // 현재 재생 중(빌려준) 이미터
    private readonly Transform _root;                      // 생성될 이미터의 부모
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 재생 중인 이미터 목록입니다. (볼륨 갱신·일괄 정지용, 읽기 전용)</summary>
    public IReadOnlyList<CSoundEmitter> Active => _active;

    /// <summary>
    /// 팩토리를 생성하고 풀을 예열합니다.
    /// </summary>
    /// <param name="root">이미터가 배치될 부모 트랜스폼</param>
    /// <param name="prewarmCount">미리 생성해둘 이미터 개수</param>
    public CSoundEmitterFactory(Transform root, int prewarmCount)
    {
        _root = root;
        for (int i = 0; i < prewarmCount; ++i)
        {
            CSoundEmitter emitter = Create();
            emitter.gameObject.SetActive(false);
            _idle.Add(emitter);
        }
    }

    /// <summary>
    /// 사용할 이미터를 빌려옵니다. 대기 이미터가 없으면 새로 생성합니다.
    /// </summary>
    public CSoundEmitter Rent()
    {
        CSoundEmitter emitter;
        int last = _idle.Count - 1;
        if (last >= 0)
        {
            emitter = _idle[last];
            _idle.RemoveAt(last);
        }
        else
        {
            emitter = Create();
        }
        emitter.gameObject.SetActive(true);
        _active.Add(emitter);
        return emitter;
    }

    /// <summary>
    /// 사용이 끝난 이미터를 풀에 반납합니다.
    /// </summary>
    public void Return(CSoundEmitter emitter)
    {
        if (emitter == null) return;

        int index = _active.IndexOf(emitter);
        if (index >= 0)
        {
            UArray.SwapLastAndRemove(_active, index); // 스왑 앤 팝
        }
        emitter.gameObject.SetActive(false);
        _idle.Add(emitter);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 새 이미터 인스턴스를 생성하고 1회 초기화합니다.
    private CSoundEmitter Create()
    {
        GameObject go = UObject.Create(K.NAME_SOUND_EMITTER, _root);
        var emitter = go.GetOrAddComponent<CSoundEmitter>();
        emitter.Setup();
        return emitter;
    }
    #endregion
}
