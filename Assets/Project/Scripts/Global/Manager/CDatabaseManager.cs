using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 고정 SO를 전역적으로 제공하는 싱글톤 클래스입니다.
/// </summary>
public class CDatabaseManager : ASingleton<CDatabaseManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly Dictionary<string, ABaseSO> _container = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    // ID로 SO를 가져오는 API
    public T Get<T>(string id) where T : ABaseSO
    {
        if (_container.TryGetValue(id, out ABaseSO so))
        {
            return so as T;
        }
        else
        {
            UDebug.Print($"데이터베이스에서 ID가 {id}인 SO가 존재하지 않습니다.", LogType.Error);
            return null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 필요한 초기화 로직 / 부모 클래스에서 자동 실행
    protected override void Initialize()
    {
        LoadScriptableObjects();
    }

    // 리소스 폴더에서 SO를 로드하여 딕셔너리에 등록
    private void LoadScriptableObjects()
    {
        ABaseSO[] datas = Resources.LoadAll<ABaseSO>(K.RESOURCE_SO_PATH);
        int length = datas.Length;

        for (int i = 0; i < length; ++i)
        {
            var data = datas[i];
            if (_container.TryAdd(data.Id, data)) continue;
            // SO 등록 실패
            UDebug.Print($"데이터베이스 초기화 작업 도중 중복되는 ID({data.Id})를 로드했습니다.", LogType.Error, data);
        }
    }
    #endregion
}
