using UnityEngine;

/// <summary>
/// 플레이어가 진입하면 스테이지 이동을 지원합니다.
/// </summary>
public class CEnterCockpit : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("키 (임시)")]
    [SerializeField] private KeyCode _upKey = KeyCode.Q;
    [SerializeField] private KeyCode _downKey = KeyCode.E;

    [Header("대상 레이어")]
    [SerializeField] private LayerMask _playerLayer;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _isInPlayer;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Update()
    {
        if (!_isInPlayer) return;
        var submarine = CStageManager.Current.SubMarine;
        if (submarine == null) return;

        if (Input.GetKeyDown(_upKey))
        {
            submarine.StartCutScene(false);
        }
        if (Input.GetKeyDown(_downKey))
        {
            submarine.StartCutScene(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.IsInLayerMask(_playerLayer))
        {
            _isInPlayer = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.IsInLayerMask(_playerLayer))
        {
            _isInPlayer = false;
        }
    }
    #endregion
}
