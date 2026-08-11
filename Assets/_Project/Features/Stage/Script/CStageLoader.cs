using System.Collections;
using UnityEngine;

/// <summary>
/// 저장 데이터가 존재할 경우 수집품을 배치합니다.
/// </summary>
public class CStageLoader : AMono
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    [Header("스테이지")]
    [SerializeField] private EScene _stage;
    [SerializeField] private bool _spawnOnStart = true;

    [Header("참조 연결")]
    [SerializeField] private CCollectibleSpawner _spawner;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public string StageID { get; private set; }
    public bool IsExistSaveData => USaveFile.Exists(StageID);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 씬의 수집품 모두 저장
    private void SaveCollectible()
    {
        CCollectible[] collectibles = UObject.FindComponents<CCollectible>(false);
        USaveFile.Save(StageID, BuildStageCollectibleData(collectibles));
    }

    // 씬에 수집품 배치
    private void LoadCollectible()
    {
        StageCollectibleData data = USaveFile.Load(StageID, new StageCollectibleData());
        if (data == null || data.soNames == null) return;

        int length = data.soNames.Length;
        for (int i = 0; i < length; ++i)
        {
            // 스포너에 확정된 데이터를 주입하여 개별 생성 지시
            _spawner.SpawnExact(
                data.soNames[i],
                data.positions[i],
                data.rotations[i],
                data.scales[i]
            );
        }
    }

    // 세이브 데이터 작성용 함수
    private StageCollectibleData BuildStageCollectibleData(CCollectible[] collectibles)
    {
        StageCollectibleData data = new StageCollectibleData();
        int length = collectibles.Length;

        // 클래스 초기화
        data.soNames = new string[length];
        data.positions = new Vector3[length];
        data.scales = new Vector3[length];
        data.rotations = new Quaternion[length];

        // 값 주입
        for (int i = 0; i < length; ++i)
        {
            Transform tr = collectibles[i].transform;
            data.soNames[i] = collectibles[i].Data.name;
            data.positions[i] = tr.position;
            data.scales[i] = tr.localScale;
            data.rotations[i] = tr.rotation;
        }

        return data;
    }

    // 부트 매니저가 준비될 때까지 대기
    private IEnumerator CreateCollectiblesCo()
    {
        while (!CBootManager.IsInitialized)
        {
            yield return null;
        }
        CreateCollectibles();
    }

    // 수집품을 불러오기 또는 생성
    private void CreateCollectibles()
    {
        // 씬 재진입
        if (IsExistSaveData)
        {
            LoadCollectible();
        }
        // 씬 최초 로딩
        else if (_spawnOnStart)
        {
            _spawner.Spawn();
        }
    }

    private void SceneLoadStartHandler(OnSceneLoadStart ctx)
    {
        SaveCollectible();
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        StageID = _stage.ToString();
    }

    private void OnEnable()
    {
        CEventBus<OnSceneLoadStart>.Subscribe(SceneLoadStartHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnSceneLoadStart>.Unsubscribe(SceneLoadStartHandler);
    }

    private void Start()
    {
        StartCoroutine(CreateCollectiblesCo());
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    // 직렬화를 위해 System.Serializable 추가 및 컴포넌트 배열 제거
    [System.Serializable]
    private class StageCollectibleData
    {
        public string[] soNames;
        public Vector3[] positions;
        public Vector3[] scales;
        public Quaternion[] rotations;
    }
    #endregion
}
