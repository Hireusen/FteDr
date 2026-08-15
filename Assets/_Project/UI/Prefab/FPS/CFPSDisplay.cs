using UnityEngine;
using TMPro;

public class CFPSDisplay : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▷ 내부 변수 ◁─────────────────────────
    [Header("UI 연결")]
    [SerializeField] private GameObject _fpsUI;
    [SerializeField] private TextMeshProUGUI _fpsText;

    [Header("설정")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.P;
    [SerializeField] private float _updateInterval = 0.5f; // 기본값 0.5초

    private bool _isEnable = true;
    private float _timer = 0f;
    private int _frameCount = 0;
    #endregion

    #region ─────────────────────────▷ 공개 함수 ◁─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    public void ExecuteUpdateFrame()
    {
        TryToggleFPS();

        // 동작 상태인가?
        if (!_isEnable) return;

        _timer += Time.unscaledDeltaTime;
        _frameCount++;

        // 일정 주기로 업데이트
        if (_timer >= _updateInterval)
        {
            // FPS 계산 및 표시
            int fps = Mathf.RoundToInt(_frameCount / _timer);
            if (_fpsText != null)
            {
                _fpsText.text = $"FPS: {fps}";
            }

            // 초기화
            _timer -= _updateInterval;
            _frameCount = 0;
        }
    }
    #endregion

    #region ─────────────────────────▷ 공개 함수 ◁─────────────────────────
    private void TryToggleFPS()
    {
        if (!Input.GetKeyDown(_toggleKey)) return;

        _isEnable = !_isEnable;
        // 처리
        if (_isEnable)
        {
            _fpsUI.SetActive(true);
            _timer = 0f;
            _frameCount = 0;
        }
        else _fpsUI.SetActive(false);
    }
    #endregion
}
