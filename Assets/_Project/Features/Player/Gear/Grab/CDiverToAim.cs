
using UnityEngine;

public class CDiverToAim : MonoBehaviour
{
    [SerializeField] private CNewGrab _newGrab;
    [SerializeField] private Transform _arm;
    [SerializeField] private Transform _playerCam;

    public (CNewGrab newGrab, Transform arm, Transform playerCam) GetReference()
    {
        return (_newGrab, _arm, _playerCam);
    }
}
