using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 오디오 클립과 볼륨 정보를 관리하는 사운드 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "SoundSO_", menuName = "ScriptableObjects/SoundSO", order = 4)]
public class CSoundSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("사운드 정보")]
    [SerializeField] protected AudioClip _clip;
    [SerializeField, Range(0f, 1f)] protected float _volume = 0.5f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>오디오 클립 원본을 반환합니다.</summary>
    public AudioClip Clip => _clip;

    /// <summary>해당 사운드의 기본 볼륨 배율을 반환합니다.</summary>
    public float Volume => _volume;

    /// <summary>
    /// 사용자 설정 볼륨을 반영한 최종 사운드 볼륨을 계산하여 반환합니다.
    /// </summary>
    /// <param name="userVolume">사용자 설정 볼륨 (0.0 ~ 1.0)</param>
    public float CalcVolume(float userVolume)
    {
        userVolume = Mathf.Clamp01(userVolume);
        return Mathf.Min(_volume * userVolume, 1f);
    }

    /// <summary>
    /// 에디터 스크립트(SoundSOGenerator)에서 자동 생성 시 호출하는 초기화 메서드입니다.
    /// </summary>
    public void InitSO(string id, AudioClip clip, float volume = 0.5f)
    {
        _type = EDataType.Sound; // EDataType에 Sound 항목이 존재해야 합니다.
        _id = id;
        _clip = clip;
        _volume = volume;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // ID를 클립의 이름으로 자동 설정
    private void AutomaticallyId()
    {
        if (_clip == null) return;

        _id = _clip.name;
    }

    // 파일명을 ID 기반으로 자동 갱신
    private void AutomaticallyFileName()
    {
#if UNITY_EDITOR
        if (_id.IsBlank()) return;

        // 이름 변경이 필요한지 확인
        string targetName = $"SoundSO_{_id}";
        if (targetName == this.name) return;

        string path = AssetDatabase.GetAssetPath(this);
        if(path.IsBlank()) return;

        // AssetDatabase 충돌 및 무한루프 방지를 위해
        // 에디터 프레임 처리가 끝난 직후 파일명을 변경하도록 예약
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;

            path = AssetDatabase.GetAssetPath(this);
            if (path.IsBlank()) return;

            AssetDatabase.RenameAsset(path, targetName);
            AssetDatabase.SaveAssets();
        };
#endif
    }

    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Sound) errorList.Add($"{errorList.Count + 1}. 타입이 Sound가 아닙니다.");
        if (_clip == null) errorList.Add($"{errorList.Count + 1}. 오디오 클립이 할당되지 않았습니다.");
        if (_volume <= 0f) errorList.Add($"{errorList.Count + 1}. 볼륨이 0 이하입니다.");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnValidate()
    {
        AutomaticallyId();
        AutomaticallyFileName();
        base.OnValidate(); // ABaseSO의 유효성 검사 로직 호출
    }

    protected override void Reset()
    {
        _type = EDataType.Sound;
    }
    #endregion
}
