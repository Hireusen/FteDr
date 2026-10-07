using Cinemachine;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CApplyCameraOption : MonoBehaviour
{
    [Header("카메라 연결")]
    [SerializeField] private CinemachineVirtualCamera _camera;

    private void OptionCameraChangeHandle(OnOptionCameraChanged ctx)
    {
        if(CGameManager.Ins.Scene == EScene.Ending)
        {
            UDebug.Print($"엔딩 씬이므로 카메라 설정 변경을 무시합니다.");
            return;
        }
        _camera.m_Lens.FarClipPlane = ctx.clipPlane;
        _camera.m_Lens.FieldOfView = ctx.fov;
    }

    private void OnEnable()
    {
        CEventBus<OnOptionCameraChanged>.Subscribe(OptionCameraChangeHandle);
    }

    private void OnDisable()
    {
        CEventBus<OnOptionCameraChanged>.Unsubscribe(OptionCameraChangeHandle);
    }

    private void Start()
    {
        var option = CLocalOptionManager.Ins.Option;
        OptionCameraChangeHandle(new OnOptionCameraChanged(option.verticalFOV, option.farClipPlane));
    }

#if UNITY_EDITOR
    [ContextMenu("카메라 거리 증가 테스트")]
    private void TestCameraOptionUp()
    {
        var local = CLocalOptionManager.Ins;
        float fov = local.Option.verticalFOV;
        float clip = local.Option.farClipPlane;
        CLocalOptionManager.Ins.SetCameraSettings(fov, clip + 10);
    }

    [ContextMenu("카메라 거리 감소 테스트")]
    private void TestCameraOptionDown()
    {
        var local = CLocalOptionManager.Ins;
        float fov = local.Option.verticalFOV;
        float clip = local.Option.farClipPlane;
        CLocalOptionManager.Ins.SetCameraSettings(fov, clip - 10);
    }
#endif
}
