using System;

/// <summary>
/// 키 리바인딩 오버라이드를 저장하기 위한 직렬화 데이터입니다.
/// Input System의 SaveBindingOverridesAsJson() 결과 문자열 하나를 담습니다.
/// </summary>
[Serializable]
public class RebindData
{
    public string overridesJson = ""; // InputActionAsset.SaveBindingOverridesAsJson() 결과
}
