#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 아이콘 생성기의 카메라 + 조명 설정을 담는 프리셋 에셋입니다.
/// 프로젝트 에셋으로 저장되어 팀원과 공유 및 버전관리(git)가 가능합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "IconPreset",
    menuName = "Tools/아이템 아이콘 생성기 프리셋",
    order = 1000)]
public class CIconGeneratorPreset : ScriptableObject
{
    [Header("해상도")]
    public int resolutionIndex = 1;   // 0=256, 1=512, 2=1024

    [Header("카메라")]
    public float pitch = 25f;
    public float yaw = 35f;
    public float padding = 1.35f;
    public bool orthographic = true;

    [Header("주광 (Key Light)")]
    public float keyIntensity = 1.6f;
    public float keyPitch = 30f;
    public float keyYaw = 40f;
    public Color keyColor = Color.white;

    [Header("보조광 (Fill Light)")]
    public float fillIntensity = 1.2f;
    public float fillPitch = -25f;
    public float fillYaw = 220f;
    public Color fillColor = new Color(0.9f, 0.9f, 1f);

    [Header("환경광 (Ambient)")]
    public Color ambientColor = new Color(0.6f, 0.6f, 0.6f, 1f);
}
#endif
