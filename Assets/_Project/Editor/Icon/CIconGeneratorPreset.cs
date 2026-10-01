#if UNITY_EDITOR
using UnityEngine;

// 아이콘 생성기 환경 설정 프리셋
[CreateAssetMenu(
    fileName = "IconPreset",
    menuName = "Tools/아이템 아이콘 생성기 프리셋",
    order = 1000)]
public class CIconGeneratorPreset : ScriptableObject
{
    [Header("출력 설정")]
    public int resolutionIndex = 1;

    [Header("카메라 설정")]
    public float pitch = 25f;
    public float yaw = 35f;
    public float padding = 1.35f;
    public bool orthographic = true;
    public Vector3 cameraOffset = Vector3.zero;

    [Header("조명 설정 - Key Light")]
    public float keyIntensity = 1.6f;
    public float keyPitch = 30f;
    public float keyYaw = 40f;
    public Color keyColor = Color.white;

    [Header("조명 설정 - Fill Light")]
    public float fillIntensity = 1.2f;
    public float fillPitch = -25f;
    public float fillYaw = 220f;
    public Color fillColor = new Color(0.9f, 0.9f, 1f);

    [Header("조명 설정 - Ambient")]
    public Color ambientColor = new Color(0.6f, 0.6f, 0.6f, 1f);
}
#endif
