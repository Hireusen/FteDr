using UnityEngine;

public class CUIObjectManager : MonoBehaviour
{
    #region ─────────────────────────▷ 인스펙터 ◁─────────────────────────
    [SerializeField] private GameObject _hudObject;
    [SerializeField] private GameObject _tutorialObject;
    [SerializeField] private GameObject _failObject;
    #endregion

    public void EnableTutorialObject()
    {
        if(_tutorialObject == null)
        {
            Debug.LogError("튜토리얼 오브젝트가 비어있습니다. 할당해주세요!");
            return;
        }
        _tutorialObject.SetActive(true);
    }

    public void DisableToturialObject()
    {
        if(_tutorialObject == null)
        {
            Debug.LogError("튜토리얼 오브젝트가 비어있습니다. 할당해주세요!");
            return;
        }
        _tutorialObject.SetActive(false);
    }

    public void EnableFailObject()
    {
        if(_failObject == null)
        {
            Debug.LogError("실패 오브젝트가 비어있습니다. 할당해주세요!");
            return;
        }
        _failObject.SetActive(true);
    }

    public void DisableFailObject()
    {
        if(_failObject == null)
        {
            Debug.LogError("실패 오브젝트가 비어있습니다. 할당해주세요!");
            return;
        }
        _failObject.SetActive(false);
    }
}
