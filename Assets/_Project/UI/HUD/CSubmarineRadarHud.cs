using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 잠수함의 화면 내/외 위치를 실시간으로 계산해 마커(방향 아이콘 + 원/원호 배지 + 거리 텍스트)를 갱신하는 HUD 컴포넌트입니다.
///
/// 하이러키 구조 (권장):
///   Marker (RectTransform, = _markerRoot) : 스크린 좌표를 따라 이동하는 루트
///     ├ Icon (RectTransform, = _iconRect) : 방향 아이콘. 화면 밖일 때만 회전
///     ├ Badge (Image, = _badgeImage) : 원/원호 겸용 이미지 하나. 스프라이트만 교체해서 씀
///     └ DistanceText (TMP_Text, = _distanceText)
///
/// - 화면 안: Badge = 원 스프라이트. 거리에 따라 스케일만 보간 (가까움=0, 중간=1, 멀리=작게)
/// - 화면 밖: Badge = 원호 스프라이트. 방향으로 회전, 가장자리에 클램프
/// - 배지가 "안 보임 → 보임"으로 전환되는 순간에만 1회 발견 연출(회전 1바퀴 + 펀치 스케일) 재생
/// </summary>
public sealed class CSubmarineRadarHud : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("참조")]
    [Tooltip("비워두면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _canvasRect;
    [Tooltip("마커 전체(아이콘+배지+텍스트)의 루트. 화면 안/밖 위치가 여기에 반영됩니다.")]
    [SerializeField] private RectTransform _markerRoot;
    [Tooltip("방향을 가리키는 아이콘의 RectTransform (화면 밖일 때만 회전)")]
    [SerializeField] private RectTransform _iconRect;
    [Tooltip("원/원호 겸용 이미지 하나. 화면 안팎에 따라 스프라이트를 교체합니다.")]
    [SerializeField] private Image _badgeImage;
    [SerializeField] private Sprite _circleSprite;
    [SerializeField] private Sprite _arcSprite;
    [SerializeField] private TMP_Text _distanceText;
    [Tooltip("마커 전체를 한 번에 켜고 끌 캔버스 그룹 (게임플레이 씬 진입/이탈 시 부드럽게 페이드)")]
    [SerializeField] private CanvasGroup _markerCanvasGroup;

    [Header("화면 안 - 거리별 배지 크기 보간")]
    [Tooltip("이 거리 이하면 배지가 완전히 안 보임")]
    [SerializeField] private float _nearDistance = 50f;
    [Tooltip("이 거리 이상이면 배지가 가장 작은 크기")]
    [SerializeField] private float _farDistance = 400f;
    [Tooltip("Near 지점에서의(=나타나는 순간) 크기")]
    [SerializeField] private float _badgeScaleMid = 1f;
    [SerializeField] private float _badgeScaleFar = 0.5f;
    [Header("원/원호 개별 크기 배율 (위 스케일에 곱해짐)")]
    [Tooltip("화면 안(원)에 적용할 배율")]
    [SerializeField] private float _circleSizeMultiplier = 1f;
    [Tooltip("화면 밖(원호)에 적용할 배율")]
    [SerializeField] private float _arcSizeMultiplier = 1f;

    [Header("발견 연출 (처음 나타날 때 1회, 0→목표크기로 커지며 회전)")]
    [SerializeField] private float _discoverSpinDuration = 0.5f;

    [Header("반복 펄스 (보이는 동안 계속, 회전 없이 살짝만)")]
    [Tooltip("이 주기(초)마다 반복 펄스를 재생합니다.")]
    [SerializeField] private float _repeatInterval = 1f;
    [SerializeField] private float _repeatPulseDuration = 0.3f;
    [SerializeField] private float _repeatPunchScale = 0.12f;

    [Header("판정 여유 (거리 경계에서 깜빡이는 것 방지)")]
    [Tooltip("한 번 보이기 시작하면, 이 여유값만큼 더 가까워져야 다시 안 보임으로 판정합니다.")]
    [SerializeField] private float _nearHysteresis = 10f;
    [Tooltip("화면 안↔밖 경계에서 판정이 깜빡이지 않도록 주는 픽셀 여유. 한 번 화면 안으로 판정되면, 이 픽셀만큼 더 벗어나야 화면 밖으로 바뀝니다.")]
    [SerializeField] private float _screenBoundsHysteresis = 40f;

    [Header("화면 가장자리 클램프 (화면 밖)")]
    [SerializeField] private float _edgePadding = 60f;

    [Header("표시 형식 / 연출")]
    [SerializeField] private string _distanceFormat = "{0:0} m";
    [SerializeField] private float _visibilityFadeDuration = 0.25f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _isMarkerVisible;
    private bool _wasBadgeVisible;   // 배지(원/원호)가 지난 프레임에 보이고 있었는지
    private bool _isPlayingDiscoverAnim;
    private bool _isInsideSubmarine = true; // 시작 시 잠수함 안이라고 가정 (CSubmarineAreaSensor가 "밖으로 나감"을 알려줘야 표시)
    private float _repeatTimer; // 배지가 보이는 동안 이 값이 _repeatInterval에 도달할 때마다 연출을 다시 재생
    private bool _wasOnScreen; // 화면 안/밖 판정에도 히스테리시스를 주기 위해 지난 프레임 상태를 기억
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    public void ExecuteUpdateFrame()
    {
        if (_isInsideSubmarine || !UScene.Current.IsGameplay() || CStageManager.Current == null || CStageManager.Current.SubMarine == null)
        {
            SetMarkerVisible(false);
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        SetMarkerVisible(true);

        Vector3 subPos = CStageManager.Current.SubMarine.transform.position;
        Vector3 camPos = _camera.transform.position;
        float distance = Vector3.Distance(camPos, subPos);

        Vector3 screenPoint = _camera.WorldToScreenPoint(subPos);
        bool inFront = screenPoint.z > 0f;

        // 화면 안이었다면 조금 더 여유(margin)를 두고 판정해, 경계에서 매 프레임 안/밖이 뒤바뀌는 것을 막는다.
        float margin = _wasOnScreen ? _screenBoundsHysteresis : 0f;
        bool withinBounds = screenPoint.x >= -margin && screenPoint.x <= Screen.width + margin
            && screenPoint.y >= -margin && screenPoint.y <= Screen.height + margin;
        bool onScreen = inFront && withinBounds;

        // 화면 밖(원호)이었다가 지금 막 화면 안(원)으로 들어온 순간인지 미리 감지해둔다.
        bool justEnteredScreen = onScreen && !_wasOnScreen;
        _wasOnScreen = onScreen;

        UpdateDistanceText(distance);

        if (onScreen)
        {
            PositionOnScreen(screenPoint);
            UpdateOnScreenBadge(distance, justEnteredScreen);
        }
        else
        {
            PositionAtEdge(screenPoint, inFront);
            UpdateOffScreenBadge();
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        if (_camera == null) _camera = Camera.main;

        CEventBus<OnPlayerSubmarineAreaChanged>.Subscribe(SubmarineAreaHandler);

        if (_circleSprite == null) UDebug.Print("CSubmarineRadarHud: Circle Sprite가 연결되지 않았습니다.", LogType.Warning, gameObject);
        if (_arcSprite == null) UDebug.Print("CSubmarineRadarHud: Arc Sprite가 연결되지 않았습니다.", LogType.Warning, gameObject);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CEventBus<OnPlayerSubmarineAreaChanged>.Unsubscribe(SubmarineAreaHandler);
    }

    private void SubmarineAreaHandler(OnPlayerSubmarineAreaChanged ctx)
    {
        _isInsideSubmarine = ctx.isInsideSubmarine;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 공통 ◀─────────────────────────
    private void UpdateDistanceText(float distance)
    {
        if (_distanceText != null)
        {
            _distanceText.text = string.Format(_distanceFormat, distance);
        }
    }

    private void PositionOnScreen(Vector3 screenPoint)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out Vector2 local))
        {
            _markerRoot.anchoredPosition = local;
        }

        // 화면 안에서는 방향 표시가 필요 없으므로, 발견 연출 중이 아닐 때만 회전을 기본값으로 되돌린다.
        if (!_isPlayingDiscoverAnim)
        {
            if (_iconRect != null) _iconRect.localRotation = Quaternion.identity;
            if (_badgeImage != null) _badgeImage.rectTransform.localRotation = Quaternion.identity;
        }
    }

    private void PositionAtEdge(Vector3 screenPoint, bool inFront)
    {
        // 카메라 뒤쪽에 있으면 스크린 좌표가 반전되어 나오므로, 방향이 뒤집히지 않도록 다시 반전시킨다.
        if (!inFront)
        {
            screenPoint.x = Screen.width - screenPoint.x;
            screenPoint.y = Screen.height - screenPoint.y;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = ((Vector2)screenPoint - screenCenter).normalized;

        float halfW = Screen.width * 0.5f - _edgePadding;
        float halfH = Screen.height * 0.5f - _edgePadding;

        float scaleX = dir.x != 0f ? halfW / Mathf.Abs(dir.x) : float.MaxValue;
        float scaleY = dir.y != 0f ? halfH / Mathf.Abs(dir.y) : float.MaxValue;
        float scale = Mathf.Min(scaleX, scaleY);

        Vector2 clampedScreenPos = screenCenter + dir * scale;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, clampedScreenPos, null, out Vector2 local))
        {
            _markerRoot.anchoredPosition = local;
        }

        if (!_isPlayingDiscoverAnim)
        {
            // 기본이 "위(0도)"를 향하고, 방향에 따라 상하좌우로 회전한다. 아이콘과 원호 둘 다 같은 각도로 회전시켜야
            // 원호가 향하는 방향과 아이콘 화살표가 어긋나지 않는다.
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            if (_iconRect != null) _iconRect.localRotation = rotation;
            if (_badgeImage != null) _badgeImage.rectTransform.localRotation = rotation;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 배지(원/원호) ◀─────────────────────────
    // 화면 안: 원 스프라이트, 거리 기준으로 스케일만 보간 (가까움=0, 중간=1, 멀리=작게)
    // justEnteredScreen: 화면 밖(원호)이었다가 방금 화면 안으로 들어온 프레임이면 true (마치 처음 본 것처럼 발견 연출을 다시 재생)
    private void UpdateOnScreenBadge(float distance, bool justEnteredScreen)
    {
        if (_badgeImage == null) return;

        // 한 번 보이면, Near보다 살짝 더 안쪽(_nearHysteresis만큼)으로 들어가야 다시 숨긴다. 경계에서 깜빡이는 것 방지.
        float effectiveNear = _wasBadgeVisible ? (_nearDistance - _nearHysteresis) : _nearDistance;
        bool shouldShow = distance > effectiveNear;

        // 지금 거리에 맞는 목표 스케일을 먼저 계산해둔다. 발견/반복 연출도 이 값을 향해 움직여야
        // "처음부터 풀사이즈로 튀었다가 다시 보간되는" 부자연스러움이 없다.
        float targetScale = ComputeOnScreenTargetScale(distance) * _circleSizeMultiplier;

        if (shouldShow)
        {
            if (!_wasBadgeVisible || justEnteredScreen)
            {
                // 처음 보이거나(안 보임→보임), 원호였다가 막 화면 안으로 들어온 경우: 발견 연출을 재생한다.
                _repeatTimer = 0f;
                PlayBadgeAnimation(targetScale, includeSpin: true);
            }
            else if (!_isPlayingDiscoverAnim)
            {
                _repeatTimer += Time.unscaledDeltaTime;
                if (_repeatTimer >= _repeatInterval)
                {
                    _repeatTimer = 0f;
                    // 계속 떠있는 동안의 반복은 회전 없이 살짝 펄스만 (너무 부산스럽지 않게)
                    PlayBadgeAnimation(targetScale, includeSpin: false);
                }
            }
        }
        _wasBadgeVisible = shouldShow;

        if (_badgeImage.sprite != _circleSprite)
        {
            _badgeImage.sprite = _circleSprite;
            _badgeImage.SetNativeSize(); // 원/원호는 원본 스프라이트 크기(비율)가 서로 다르므로 교체 시마다 재설정
        }
        _badgeImage.enabled = shouldShow;

        if (!shouldShow || _isPlayingDiscoverAnim) return; // 발견 연출이 스케일을 대신 제어하는 동안은 덮어쓰지 않는다

        _badgeImage.rectTransform.localScale = Vector3.one * targetScale;
    }

    // 거리 하나만 가지고 목표 스케일을 계산한다. (크기 배율 곱하기 전, 순수 보간값)
    // Near에서 가장 크게(=_badgeScaleMid) 나타나서, Far로 갈수록 계속 작아지기만 한다. (중간에 커지는 구간 없음)
    private float ComputeOnScreenTargetScale(float distance)
    {
        float t = Mathf.InverseLerp(_nearDistance, _farDistance, distance);
        return Mathf.Lerp(_badgeScaleMid, _badgeScaleFar, t);
    }

    // 화면 밖: 원호 스프라이트, 방향 표시용이라 거리 상관없이 항상 보통 크기로 고정
    private void UpdateOffScreenBadge()
    {
        if (_badgeImage == null) return;

        if (!_wasBadgeVisible)
        {
            _repeatTimer = 0f;
            PlayBadgeAnimation(_badgeScaleMid * _arcSizeMultiplier, includeSpin: true);
        }
        else if (!_isPlayingDiscoverAnim)
        {
            _repeatTimer += Time.unscaledDeltaTime;
            if (_repeatTimer >= _repeatInterval)
            {
                _repeatTimer = 0f;
                PlayBadgeAnimation(_badgeScaleMid * _arcSizeMultiplier, includeSpin: false);
            }
        }
        _wasBadgeVisible = true;

        if (_badgeImage.sprite != _arcSprite)
        {
            _badgeImage.sprite = _arcSprite;
            _badgeImage.SetNativeSize();
        }
        _badgeImage.enabled = true;

        if (!_isPlayingDiscoverAnim)
        {
            _badgeImage.rectTransform.localScale = Vector3.one * (_badgeScaleMid * _arcSizeMultiplier);
        }
    }

    // includeSpin=true: 처음 등장 시의 발견 연출. 0에서 목표 크기까지 커지는 동안 동시에 한 바퀴 회전한다.
    // includeSpin=false: 계속 떠있는 동안의 반복 알림 (회전 없이 작은 펀치스케일 펄스만)
    private void PlayBadgeAnimation(float targetScale, bool includeSpin)
    {
        if (_badgeImage == null) return;

        _isPlayingDiscoverAnim = true;

        RectTransform rect = _badgeImage.rectTransform;
        rect.DOKill();

        Sequence seq = DOTween.Sequence();

        if (includeSpin)
        {
            // 0에서 목표 크기까지 커지는 동안 정확히 한 바퀴(360도) 회전
            rect.localScale = Vector3.zero;
            rect.localRotation = Quaternion.identity;

            seq.Join(rect.DOScale(targetScale, _discoverSpinDuration).SetEase(Ease.OutBack));
            seq.Join(rect.DORotate(new Vector3(0f, 0f, 360f), _discoverSpinDuration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
        }
        else
        {
            rect.localScale = Vector3.one * targetScale;
            seq.Join(rect.DOPunchScale(Vector3.one * _repeatPunchScale, _repeatPulseDuration, vibrato: 1, elasticity: 0.5f));
        }

        seq.SetUpdate(true);
        seq.OnComplete(() =>
        {
            if (includeSpin) rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * targetScale;
            _isPlayingDiscoverAnim = false;
        });
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 마커 전체 표시 ◀─────────────────────────
    private void SetMarkerVisible(bool visible)
    {
        if (_isMarkerVisible == visible) return;
        _isMarkerVisible = visible;

        if (!visible) _wasBadgeVisible = false; // 다시 나타날 때 발견 연출이 재생되도록 리셋

        if (_markerCanvasGroup == null) return;

        _markerCanvasGroup.DOKill();
        _markerCanvasGroup.DOFade(visible ? 1f : 0f, _visibilityFadeDuration).SetUpdate(true);
    }
    #endregion
}
