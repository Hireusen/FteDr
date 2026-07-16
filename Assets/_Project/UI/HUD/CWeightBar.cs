using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 가방 무게 게이지(Weight_Slider)를 갱신하는 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CWeightBar : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("가방 무게 게이지 설정")]
    [Tooltip("Fill Amount를 조절할 이미지 (Image Type: Filled)")]
    [SerializeField] private Image _fillImage;
    [Tooltip("'0 / 0 KG' 형태로 표시할 텍스트")]
    [SerializeField] private TMP_Text _weightText;

    [Header("표시 형식")]
    [SerializeField] private string _format = "총 무게 {0:0.#} / {1:0.#} KG";
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnPlayerWeightChanged>.Subscribe(WeightChangedHandler);

        // 창을 여는 시점에 현재 무게 상태를 즉시 반영
        Refresh(UPlayer.CurrentWeight, UPlayer.MaxWeight);
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerWeightChanged>.Unsubscribe(WeightChangedHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void WeightChangedHandler(OnPlayerWeightChanged data)
    {
        Refresh(data.currentWeight, data.maxWeight);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Refresh(float current, float max)
    {
        if (_fillImage != null)
        {
            _fillImage.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        if (_weightText != null)
        {
            _weightText.text = string.Format(_format, current, max);
        }
    }
    #endregion
}
