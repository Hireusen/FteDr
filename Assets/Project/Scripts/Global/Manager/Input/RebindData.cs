using System;

/// <summary>
/// 키 리바인딩 오버라이드를 저장하기 위한 직렬화 데이터입니다.
/// </summary>
[Serializable]
public class RebindData
{
    // InputActionAsset.SaveBindingOverridesAsJson() 결과
    public string overridesJson = "";
}
