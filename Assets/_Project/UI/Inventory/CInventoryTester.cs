#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// 런타임에 인벤토리 상태를 확인하고 아이템을 테스트 획득해보는 개발용 미니 패널입니다.
/// </summary>
public sealed class CInventoryTester : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("표시 설정")]
    [SerializeField] private int _fontSize = 24; // 글자 크기

    [Header("인벤토리 테스트용 수집품 목록")]
    [Tooltip("버튼으로 바로 가방에 넣어볼 수집품 ID 목록")]
    [SerializeField] private string[] _testCollectibleIds = { Id.Collectible_Amulet1_Aged_Gold, Id.Collectible_Amulet1_Aged_Mixed, Id.Collectible_Amulet1_Fine_Bronze, Id.Collectible_Amulet1_Fine_Metal };
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const int WINDOW_ID = 998811; // 인벤토리 테스터용 고유 ID
    private Vector2 _scroll;
    private Rect _window = new Rect(20, 20, 450, 500); // 콤팩트해진 창 크기

    private GUISkin _skin;
    private int _appliedFontSize = -1;

    // 인벤토리 정보 캐시
    private bool _open;
    private int _bagCount, _bagCap;
    private float _weightCur, _weightMax;
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

    private void DrawWindow(int id)
    {
        _scroll = GUILayout.BeginScrollView(_scroll);

        // 1. 현재 상태 출력
        GUILayout.Box($"가방 칸: {_bagCount} / {_bagCap}\n무게: {_weightCur:F1} / {_weightMax:F1} KG");
        GUILayout.Space(10);

        // 2. 가방 비우기 기능
        if (GUILayout.Button("가방 전체 비우기"))
        {
            //UPlayer.ClearBag();
        }
        GUILayout.Space(15);

        // 3. 아이템 생성 버튼 리스트
        GUILayout.Label("── 테스트 아이템 생성 ──");
        if (_testCollectibleIds == null || _testCollectibleIds.Length == 0)
        {
            GUILayout.Label("등록된 테스트 ID가 없습니다.");
        }
        else
        {
            int length = _testCollectibleIds.Length;
            for (int i = 0; i < length; ++i)
            {
                string collectibleId = _testCollectibleIds[i];
                if (collectibleId.IsBlank()) continue;

                CCollectibleSO so = UData.Collectible(collectibleId);
                string btnLabel = so != null ? $"➕ {so.Name} ({so.Weight}KG)" : $"➕ {collectibleId} (SO 누락)";

                if (GUILayout.Button(btnLabel))
                {
                    if (!UPlayer.TryAddToBag(collectibleId))
                    {
                        UDebug.Print($"[테스터] '{collectibleId}' 획득 실패 (슬롯 부족 또는 무게 초과)", LogType.Warning);
                    }
                }
            }
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 20)); // 상단 바 드래그 이동 가능
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
