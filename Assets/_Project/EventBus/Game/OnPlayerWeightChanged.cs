/// <summary>
/// 가방에 담긴 아이템의 무게가 변경될 때 발생하는 이벤트 구조체입니다.
/// </summary>
public readonly struct OnPlayerWeightChanged
{
    public readonly float currentWeight;
    public readonly float maxWeight;

    public OnPlayerWeightChanged(float currentWeight, float maxWeight)
    {
        this.currentWeight = currentWeight;
        this.maxWeight = maxWeight; 
    }

    /// <param name="currentWeight">현재 무게</param>
    /// <param name="maxWeight">최대 무게</param>
    public static void Publish(float currentWeight, float maxWeight)
    {
        CEventBus<OnPlayerWeightChanged>.Publish(new OnPlayerWeightChanged(currentWeight, maxWeight));
    }   
}
