using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 두 노드(Dot) 사이를 잇는 선을 코드로 맞춰주는 컴포넌트입니다.
/// 선 이미지를 미리 정확한 위치/길이로 배치해둘 필요 없이, 양 끝 Dot의 위치만 보고
/// 중점·길이·각도를 계산해 선 RectTransform에 적용합니다. 노드 간격이나 해상도가 바뀌어도 알아서 맞습니다.
///
/// 선은 단순한 사각형이 아니라 끝 장식이 달린 그래픽이므로, 늘어나도 장식이 뭉개지지 않도록
/// 스프라이트를 9-slice(Image Type: Sliced)로 설정하고 Border를 잡아두세요.
/// 높이는 기본적으로 건드리지 않고 프리팹에 잡아둔 값을 그대로 씁니다.
///
/// 아직 연구되지 않은 구간은 Locked 색(회색 + 낮은 투명도)으로 흐리게 표시합니다.
/// 여러 조각(몸통 + 끝 장식 등)으로 나뉘어 있으면 전부 등록해 한꺼번에 맞출 수 있습니다.
///
/// 비용은 갱신 1회당 Vector2 연산 몇 번뿐이라 무시해도 되지만, 캔버스 리빌드를 유발하므로
/// 매 프레임 호출하지 말고 내용이 실제로 바뀔 때만 Refresh를 호출하세요.
/// </summary>
[DisallowMultipleComponent]
public sealed class CStageNodeLink : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이을 두 지점")]
    [Tooltip("선이 시작되는 노드의 Dot")]
    [SerializeField] private RectTransform _from;
    [Tooltip("선이 끝나는 노드의 Dot. 끝 장식이 있는 비대칭 그래픽이면 이쪽이 장식이 향할 방향입니다.")]
    [SerializeField] private RectTransform _to;

    [Header("선 그래픽")]
    [Tooltip("위치/길이/각도/색을 맞출 선 이미지들. 몸통과 끝 장식이 나뉘어 있으면 전부 등록하세요.")]
    [SerializeField] private Graphic[] _lineGraphics;
    [Tooltip("양 끝에서 Dot을 피해 띄울 여백(픽셀). Dot 반지름 정도를 넣으면 깔끔합니다.")]
    [SerializeField, Min(0f)] private float _endPadding = 0f;
    [Tooltip("체크하면 선 높이도 아래 값으로 덮어씁니다. 보통은 꺼두고 프리팹에 잡아둔 높이를 씁니다.")]
    [SerializeField] private bool _overrideHeight = false;
    [SerializeField, Min(0f)] private float _height = 4f;

    [Header("상태별 색")]
    [Tooltip("해금된 구간의 색")]
    [SerializeField] private Color _activeColor = new Color(0.20f, 0.85f, 0.95f, 1f);
    [Tooltip("아직 연구하지 않은 구간의 색. 알파를 낮춰 흐리게 둡니다.")]
    [SerializeField] private Color _lockedColor = new Color(0.45f, 0.48f, 0.52f, 0.35f);
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 두 Dot의 현재 위치를 다시 읽어 선을 맞추고, 연구 여부에 따른 색을 입힙니다.
    /// 레이아웃 그룹이 노드를 배치한 뒤에 호출해야 좌표가 최신입니다.
    /// (CStageTravelController가 Canvas.ForceUpdateCanvases 후에 호출합니다)
    /// </summary>
    /// <summary>
    /// 선 그래픽을 보이거나 숨깁니다. 이전/다음 해역이 없는 끝 구간에서 선만 덩그러니 남지 않도록 씁니다.
    /// 오브젝트를 껐다 켜지 않고 그래픽만 끄므로 레이아웃에 영향을 주지 않습니다.
    /// </summary>
    /// <param name="visible">표시 여부</param>
    public void SetVisible(bool visible)
    {
        if (_lineGraphics == null) return;

        for (int i = 0; i < _lineGraphics.Length; ++i)
        {
            if (_lineGraphics[i] != null) _lineGraphics[i].enabled = visible;
        }
    }

    /// <param name="locked">true면 아직 연구되지 않은 구간으로 흐리게 표시</param>
    public void Refresh(bool locked)
    {
        if (_lineGraphics == null) return;

        Color color = locked ? _lockedColor : _activeColor;

        for (int i = 0; i < _lineGraphics.Length; ++i)
        {
            Graphic graphic = _lineGraphics[i];
            if (graphic == null) continue;

            graphic.color = color;
            ApplyTransform(graphic.rectTransform);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 선 하나를 두 Dot 사이에 눕힌다. 선의 부모 기준 좌표로 변환해서 계산하므로
    // 선이 어느 부모 밑에 있든(노드 바깥의 별도 컨테이너여도) 그대로 동작한다.
    private void ApplyTransform(RectTransform line)
    {
        if (line == null || _from == null || _to == null) return;

        Transform parent = line.parent;
        if (parent == null)
        {
            UDebug.Print("선 오브젝트에 부모가 없습니다.", LogType.Error, line.gameObject);
            return;
        }

        Vector2 start = parent.InverseTransformPoint(_from.position);
        Vector2 end = parent.InverseTransformPoint(_to.position);

        Vector2 delta = end - start;

        // 두 Dot이 겹쳐 있으면(아직 레이아웃 전 등) 길이가 음수가 되지 않게 막는다.
        float length = Mathf.Max(0f, delta.magnitude - _endPadding * 2f);

        line.localPosition = (start + end) * 0.5f;
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        // 가로 길이만 늘린다. 높이를 건드리면 끝 장식 비율이 깨지므로 기본적으로 유지한다.
        line.sizeDelta = new Vector2(length, _overrideHeight ? _height : line.sizeDelta.y);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
#if UNITY_EDITOR
    // 에디터에서 값을 만지는 동안에도 바로 보이게 한다. (플레이 중에는 Refresh로만 갱신)
    private void OnValidate()
    {
        if (Application.isPlaying || _lineGraphics == null) return;

        // OnValidate 안에서 RectTransform 크기를 바꾸면, Unity가 OnRectTransformDimensionsChange를
        // SendMessage로 전파하려다 "SendMessage cannot be called during OnValidate" 경고를 띄운다.
        // 그래서 적용을 한 프레임 미뤄 OnValidate가 끝난 뒤에 처리한다.
        UnityEditor.EditorApplication.delayCall += ApplyInEditor;
    }

    // delayCall은 에디터가 한 번 호출하고 비우므로 따로 해제하지 않는다.
    // 대기하는 사이에 오브젝트가 지워졌을 수 있어 자신이 아직 살아있는지만 확인한다.
    private void ApplyInEditor()
    {
        if (this == null || Application.isPlaying || _lineGraphics == null) return;

        for (int i = 0; i < _lineGraphics.Length; ++i)
        {
            if (_lineGraphics[i] != null) ApplyTransform(_lineGraphics[i].rectTransform);
        }
    }
#endif
    #endregion
}
