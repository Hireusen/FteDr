using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 슬롯에 마우스를 올렸을 때 우측에 뜨는 아이템 툴팁입니다.
/// 등급(ECollectibleRarity)에 따라 테두리/이름 색상을 실시간으로 매핑합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CItemTooltip : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("툴팁 설정")]
    [Tooltip("이 툴팁의 루트 RectTransform. Pivot을 (0, 1)로 두면 마우스 기준 우측 정렬됩니다.")]
    [SerializeField] private RectTransform _root;
    [Tooltip("표시/숨김에 사용할 캔버스 그룹")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("좌표 변환 기준이 되는 캔버스")]
    [SerializeField] private Canvas _canvas;

    [Header("데이터 바인딩")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _weightText;
    [SerializeField] private TMP_Text _sellPriceText;
    [SerializeField] private TMP_Text _rarityText;
    [Tooltip("등급별 테두리 이미지 (배경/외곽선용)")]
    [SerializeField] private Image _borderImage;

    [Header("표시 형식")]
    [SerializeField] private string _weightFormat = "{0:0.#} KG";
    [SerializeField] private string _sellPriceFormat = "{0:0} G";
    [Tooltip("마우스 위치로부터 얼마나 떨어뜨려 표시할지")]
    [SerializeField] private Vector2 _mouseOffset = new Vector2(16f, -16f);

    [Header("등급 색상 매핑")]
    [SerializeField] private List<RarityVisualData> _rarityVisuals = new();

    [Header("조준모드에 띄우는건지 확인")]
    [SerializeField] private bool _zozunMode = false;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Dictionary<ECollectibleRarity, RarityVisualData> _rarityLookup;
    private bool _isVisible;
    
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    // 프레임 매니저에게 호출당할 함수 (표시 중일 때만 마우스를 따라감)
    public void ExecuteUpdateFrame()
    {
        if (!_isVisible) return;
        if (_zozunMode) return;
        UpdatePosition(Input.mousePosition);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _rarityLookup = new Dictionary<ECollectibleRarity, RarityVisualData>();
        int count = _rarityVisuals.Count;
        for (int i = 0; i < count; ++i)
        {
            _rarityLookup[_rarityVisuals[i].rarity] = _rarityVisuals[i];
        }

        Hide();
    }

    // 인스펙터에서 새 등급 항목을 추가하면 Color 필드가 (0,0,0,0)으로 초기화되어
    // 알파가 0인 채로 남는 경우가 많다. 편집 시점에 알파 0을 1로 자동 보정한다.
    private void OnValidate()
    {
        int count = _rarityVisuals.Count;
        for (int i = 0; i < count; ++i)
        {
            RarityVisualData visual = _rarityVisuals[i];

            if (visual.borderColor.a <= 0f) visual.borderColor.a = 1f;
            if (visual.textColor.a <= 0f) visual.textColor.a = 1f;

            _rarityVisuals[i] = visual;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        CEventBus<OnRequestShowTooltip>.Subscribe(ShowHandler);
        CEventBus<OnRequestHideTooltip>.Subscribe(HideHandler);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnRequestShowTooltip>.Unsubscribe(ShowHandler);
        CEventBus<OnRequestHideTooltip>.Unsubscribe(HideHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void ShowHandler(OnRequestShowTooltip data)
    {
        Bind(data.data);
        UpdatePosition(data.screenPosition);
        Show();
    }

    private void HideHandler(OnRequestHideTooltip data)
    {
        Hide();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // SO 데이터를 텍스트/이미지/등급 색상에 반영합니다.
    private void Bind(CCollectibleSO data)
    {
        if (data == null) return;

        if (_iconImage != null) _iconImage.sprite = data.Icon;
        if (_descriptionText != null) _descriptionText.text = data.Description;
        if (_weightText != null) _weightText.text = string.Format(_weightFormat, data.Weight);
        if (_sellPriceText != null) _sellPriceText.text = string.Format(_sellPriceFormat, data.SellPrice);
        if (_rarityText != null) _rarityText.text = data.CollectibleRarity.ToString();

        RarityVisualData visual = GetRarityVisual(data.CollectibleRarity);

        if (_nameText != null)
        {
            _nameText.text = data.Name;
            _nameText.color = visual.textColor;
        }
        if (_borderImage != null)
        {
            _borderImage.color = visual.borderColor;
        }
    }

    // 등급에 매핑된 색상 데이터를 반환합니다. 매핑이 없으면 기본값(흰색)을 반환합니다.
    private RarityVisualData GetRarityVisual(ECollectibleRarity rarity)
    {
        if (_rarityLookup != null && _rarityLookup.TryGetValue(rarity, out RarityVisualData visual))
        {
            return visual;
        }

        UDebug.Print($"CItemTooltip: 등급({rarity})에 대한 색상 매핑이 없습니다.", LogType.Warning, gameObject);
        return new RarityVisualData { rarity = rarity, borderColor = Color.white, textColor = Color.white };
    }

    // 스크린 좌표를 기준으로 툴팁 위치를 캔버스 로컬 좌표로 변환해 반영합니다.
    private void UpdatePosition(Vector2 screenPosition)
    {
        if (_root == null || _canvas == null) return;

        RectTransform canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPoint))
        {
            _root.anchoredPosition = localPoint + _mouseOffset;
        }
    }

    private void Show()
    {
        _isVisible = true;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    private void Hide()
    {
        _isVisible = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    /// <summary>
    /// 등급 하나에 대한 테두리/텍스트 색상 매핑 데이터입니다.
    /// </summary>
    [Serializable]
    public struct RarityVisualData
    {
        public ECollectibleRarity rarity;
        public Color borderColor;
        public Color textColor;
    }
    #endregion
}
