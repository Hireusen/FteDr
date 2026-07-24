#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// CSoundSO의 커스텀 인스펙터입니다.
/// 플레이 모드 없이 에디터에서 사운드를 미리 듣게 하며, 개별(SO) 로우패스와 볼륨을 실제로 반영합니다.
/// 프리뷰용 임시 오브젝트는 씬에 직렬화되지 않고(HideAndDontSave), 재생 종료·플레이 진입·
/// 도메인 리로드·씬 저장 직전에 자동 정리되어 씬에 잔존하지 않습니다.
/// </summary>
[CustomEditor(typeof(CSoundSO))]
public class CSoundSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var so = (CSoundSO)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("프리뷰", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(so.Clip == null))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("▶ 재생", GUILayout.Height(24)))
            {
                CSoundSOPreview.Play(so);
            }
            if (GUILayout.Button("■ 정지", GUILayout.Height(24)))
            {
                CSoundSOPreview.Stop();
            }
        }

        if (so.Clip == null)
        {
            EditorGUILayout.HelpBox("오디오 클립이 없어 프리뷰할 수 없습니다.", MessageType.Info);
        }
        else
        {
            string lp = so.UseLowPass ? $"로우패스 ON ({so.LowPassCutoff:0}Hz)" : "로우패스 OFF";
            EditorGUILayout.HelpBox(
                $"볼륨 {so.Volume:0.00} · {lp}\n" +
                "개별 로우패스·볼륨이 반영됩니다. (거리감·전역 로우패스는 런타임 전용이라 미반영)",
                MessageType.None);
        }
    }
}

/// <summary>
/// 에디터 프리뷰 재생·정리를 담당하는 정적 헬퍼입니다.
/// 임시 오브젝트를 하나만 유지하며, 여러 안전장치로 씬 잔존을 방지합니다.
/// </summary>
[InitializeOnLoad]
internal static class CSoundSOPreview
{
    private static GameObject _go;      // 프리뷰용 임시 오브젝트 (씬에 저장되지 않음)
    private static AudioSource _source;
    private static AudioLowPassFilter _lowPass;

    // 에디터 로드 시 자동 정리 훅을 등록합니다.
    static CSoundSOPreview()
    {
        // 재생이 자연 종료됐는지 매 에디터 프레임 확인
        EditorApplication.update += OnEditorUpdate;
        // 플레이 모드 전환·도메인 리로드 직전 정리
        EditorApplication.playModeStateChanged += (state) => Stop();
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        // 씬 저장 직전 정리 (혹시 남아 있어도 직렬화 대상에서 제거)
        EditorSceneManager.sceneSaving += (scene, path) => Stop();
    }

    /// <summary>지정한 SO를 개별 로우패스·볼륨을 반영해 재생합니다.</summary>
    public static void Play(CSoundSO so)
    {
        if (so == null || so.Clip == null) return;

        Stop(); // 이전 프리뷰 정리 (임시 오브젝트는 항상 하나만 유지)
        EnsureObject();

        // 2D로 재생해 거리감 없이 원음+필터를 그대로 들려줍니다.
        _source.clip = so.Clip;
        _source.volume = Mathf.Clamp01(so.Volume);
        _source.spatialBlend = 0f;
        _source.loop = false;

        // 개별(SO) 로우패스만 반영 (전역 로우패스는 런타임 개념)
        _lowPass.enabled = so.UseLowPass;
        if (so.UseLowPass) _lowPass.cutoffFrequency = so.LowPassCutoff;

        _source.Play();
    }

    /// <summary>프리뷰를 정지하고 임시 오브젝트를 즉시 파괴합니다.</summary>
    public static void Stop()
    {
        if (_source != null) _source.Stop();

        if (_go != null)
        {
            // 에디터에서는 DestroyImmediate로 즉시 제거
            Object.DestroyImmediate(_go);
        }
        _go = null;
        _source = null;
        _lowPass = null;
    }

    // 재생이 자연 종료되면 임시 오브젝트를 자동 정리합니다.
    private static void OnEditorUpdate()
    {
        if (_go == null || _source == null) return;

        if (!_source.isPlaying)
        {
            Stop();
        }
    }

    // 임시 오브젝트를 생성합니다. HideAndDontSave로 씬 직렬화에서 제외됩니다.
    private static void EnsureObject()
    {
        if (_go != null) return;

        _go = new GameObject("@SoundPreview(Editor)")
        {
            // 하이어라키에 표시하지 않고, 씬 저장 시 직렬화하지 않음
            hideFlags = HideFlags.HideAndDontSave
        };
        _source = _go.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _lowPass = _go.AddComponent<AudioLowPassFilter>();
        _lowPass.enabled = false;
    }
}
#endif
