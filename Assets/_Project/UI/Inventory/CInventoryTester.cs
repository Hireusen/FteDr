#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// 런타임에 인벤토리 상태를 확인하고, 등록된 풀에서 무작위로 하나를 뽑아 테스트 획득해보는 개발용 미니 패널입니다.
/// </summary>
public sealed class CInventoryTester : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("표시 설정")]
    [SerializeField] private int _fontSize = 24; // 글자 크기

    [Header("랜덤 획득 풀")]
    [Tooltip("'랜덤 획득' 버튼을 누르면 이 중 하나를 무작위로 뽑아 가방에 넣습니다.")]
    [SerializeField]
    private string[] _testCollectibleIds =
    {
        Id.Collectible_Amulet1_Aged_Gold,
        Id.Collectible_AmuletGem_Cyan_Heart,
        Id.Collectible_WF_GreekRelics_CorinthianHelmet,
        Id.Collectible_WF_GreekRelics_PaintedAmphora
    };
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const int WINDOW_ID = 998811; // 인벤토리 테스터용 고유 ID
    private Vector2 _scroll;
    private Rect _window = new Rect(20, 20, 420, 320); // 컴팩트한 창 크기

    private GUISkin _skin;
    private int _appliedFontSize = -1;

    // 인벤토리 정보 캐시
    private bool _open;
    private int _bagCount, _bagCap;
    private float _weightCur, _weightMax;

    // 마지막으로 뽑힌 아이템 정보 캐시 (결과 확인용)
    private CCollectibleSO _lastPicked;
    private bool _lastPickSucceeded;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void RefreshCache()
    {
        _bagCount = UPlayer.BagItems.Count;
        _bagCap = UPlayer.BagCapacity;
        _weightCur = UPlayer.CurrentWeight;
        _weightMax = UPlayer.MaxWeight;
    }

    private void BagHandler(OnPlayerBagChanged ctx) { _bagCount = ctx.count; _bagCap = ctx.capacity; }
    private void WeightHandler(OnPlayerWeightChanged ctx) { _weightCur = ctx.currentWeight; _weightMax = ctx.maxWeight; }
    private void CheatHandler(OnInputCheat ctx) { _open = !_open; }

    private void ApplyFontSize()
    {
        if (_skin == null) _skin = Instantiate(GUI.skin);

        if (_appliedFontSize != _fontSize)
        {
            _skin.label.fontSize = _fontSize;
            _skin.button.fontSize = _fontSize;
            _skin.box.fontSize = _fontSize;
            _skin.window.fontSize = _fontSize;
            _appliedFontSize = _fontSize;
        }

        GUI.skin = _skin;
    }

    // CPlayerManager/UPlayer에 전용 메서드를 추가하지 않고, 이미 공개된 Runtime을 직접 비운다.
    // 매니저의 Publish 경로를 거치지 않으므로, UI 갱신을 위해 이벤트를 직접 재발행한다.
    private void ClearBagDebug()
    {
        CPlayerManager manager = CPlayerManager.Ins;
        if (manager == null) return;

        manager.Runtime.bagItems.Clear();

        OnPlayerBagChanged.Publish(0, UPlayer.BagCapacity);
        OnPlayerWeightChanged.Publish(0f, UPlayer.MaxWeight);
    }

    // 풀에서 하나를 무작위로 뽑아 획득을 시도하고, 결과를 캐시에 남긴다.
    private void PickRandomAndAcquire()
    {
        if (_testCollectibleIds == null || _testCollectibleIds.Length == 0)
        {
            UDebug.Print("[인벤토리 테스터] 등록된 테스트 ID 풀이 비어있습니다.", LogType.Warning);
            return;
        }

        string id = _testCollectibleIds[Random.Range(0, _testCollectibleIds.Length)];
        _lastPicked = UData.Collectible(id);
        _lastPickSucceeded = UPlayer.TryAddToBag(id);

        if (!_lastPickSucceeded)
        {
            UDebug.Print($"[인벤토리 테스터] '{id}' 획득 실패 (슬롯 부족 또는 무게 초과)", LogType.Warning);
        }
    }

    private void DrawWindow(int id)
    {
        _scroll = GUILayout.BeginScrollView(_scroll);

        // 1. 현재 가방 상태
        GUILayout.Box($"가방 칸: {_bagCount} / {_bagCap}\n무게: {_weightCur:F1} / {_weightMax:F1} KG");
        GUILayout.Space(10);

        // 2. 조작 버튼
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("랜덤 획득")) PickRandomAndAcquire();
        if (GUILayout.Button("가방 비우기")) ClearBagDebug();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // 3. 풀 미리보기 (등급/무게가 안 바뀌는 것처럼 보일 때 원인 진단용)
        DrawPoolPreviewSection();
        GUILayout.Space(10);

        // 4. 마지막으로 뽑힌 아이템 데이터 표시
        DrawLastPickedSection();

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 20)); // 상단 바 드래그 이동 가능
    }

    // 풀에 등록된 ID들이 실제로 어떤 데이터로 resolve되는지 보여준다.
    // InstanceID가 겹치면 ID 등록 버그, 등급/무게 값 자체가 같으면 에셋에 데이터가 아직 다르게 입력되지 않은 것이다.
    private void DrawPoolPreviewSection()
    {
        GUILayout.Label("── 풀 미리보기 (등급/무게가 안 바뀔 때 원인 확인용) ──");

        if (_testCollectibleIds == null || _testCollectibleIds.Length == 0)
        {
            GUILayout.Label("등록된 테스트 ID가 없습니다.");
            return;
        }

        int length = _testCollectibleIds.Length;
        for (int i = 0; i < length; ++i)
        {
            string collectibleId = _testCollectibleIds[i];
            if (collectibleId.IsBlank()) continue;

            CCollectibleSO so = UData.Collectible(collectibleId);
            if (so == null)
            {
                GUILayout.Label($"{collectibleId} → SO 없음");
                continue;
            }

            GUILayout.Label($"{collectibleId} → {so.Name} | {so.CollectibleRarity} | {so.Weight}KG | InstanceID {so.GetInstanceID()}");
        }
    }

    private void DrawLastPickedSection()
    {
        GUILayout.Label("── 마지막으로 뽑힌 아이템 ──");

        if (_lastPicked == null)
        {
            GUILayout.Label("아직 뽑은 적이 없습니다.");
            return;
        }

        string resultLabel = _lastPickSucceeded ? "획득 성공" : "획득 실패 (슬롯/무게 초과)";
        GUILayout.Box(
            $"{resultLabel}\n" +
            $"이름: {_lastPicked.Name}\n" +
            $"등급: {_lastPicked.CollectibleRarity}\n" +
            $"무게: {_lastPicked.Weight} KG\n" +
            $"판매가: {_lastPicked.SellPrice} G\n" +
            $"설명: {_lastPicked.Description}");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        RefreshCache();
        CEventBus<OnPlayerBagChanged>.Subscribe(BagHandler);
        CEventBus<OnPlayerWeightChanged>.Subscribe(WeightHandler);
        CEventBus<OnInputCheat>.Subscribe(CheatHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerBagChanged>.Unsubscribe(BagHandler);
        CEventBus<OnPlayerWeightChanged>.Unsubscribe(WeightHandler);
        CEventBus<OnInputCheat>.Unsubscribe(CheatHandler);
    }

    private void OnGUI()
    {
        if (!_open) return;

        ApplyFontSize();
        _window = GUI.Window(WINDOW_ID, _window, DrawWindow, "인벤토리 디버그 컨트롤러 (F1)");
    }
    #endregion
}
#endif
