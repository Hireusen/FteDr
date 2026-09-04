
using UnityEngine;

public class CDiverToAim : MonoBehaviour
{
    [SerializeField] private CNewGrab _newGrab;
    [SerializeField] private Transform _arm;
    [SerializeField] private Transform _playerCam;
    public CAimShow AimCanvas { get;private set; }
    public (CNewGrab newGrab, Transform arm, Transform playerCam) GetReference(CAimShow aimCanvas)
    {
        AimCanvas = aimCanvas;
        return (_newGrab, _arm, _playerCam);
    }

}
