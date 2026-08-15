
using UnityEngine;

public class CDiverToAim : MonoBehaviour
{
    [SerializeField] private CNewGrab _newGrab;
    [SerializeField] private Transform _arm;
    [SerializeField] private Transform _playerCam;
    public GameObject AimCanvas { get;private set; }
    public (CNewGrab newGrab, Transform arm, Transform playerCam) GetReference(GameObject aimCanvas)
    {
        AimCanvas = aimCanvas;
        return (_newGrab, _arm, _playerCam);
    }

}
