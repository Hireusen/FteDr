using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CPlayer : MonoBehaviour
{
    /*
     여러가지 코드들
     */

    [SerializeField] private CUIObjectManager _uiObjectManager;

    private void Awake()
    {
        if(_uiObjectManager == null)
        {
            Debug.LogError("UI 오브젝트 매니저가 비어있습니다. 할당해주세요!");
            enabled = false; // 본인을 비활성화하겠다.
            return;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _uiObjectManager.EnableTutorialObject();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            _uiObjectManager.DisableToturialObject();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _uiObjectManager.EnableFailObject();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            _uiObjectManager.DisableFailObject();
        }
    }
}
