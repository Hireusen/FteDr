/// <summary>
/// 사용자 볼륨을 변경할 경우 발행합니다.
/// </summary>
public readonly struct OnOptionVolumeChanged
{
    public readonly float master;
    public readonly float sfx;
    public readonly float bgm;
    public readonly float ambience;

    public OnOptionVolumeChanged(float master, float sfx, float bgm, float ambience)
    {
        this.master = master;
        this.sfx = sfx;
        this.bgm = bgm;
        this.ambience = ambience;
    }

    /// <param name="master">마스터 볼륨 (0.0 ~ 1.0)</param>
    /// <param name="sfx">SFX 볼륨 (0.0 ~ 1.0)</param>
    /// <param name="bgm">BGM 볼륨 (0.0 ~ 1.0)</param>
    /// <param name="ambience">환경음 볼륨 (0.0 ~ 1.0)</param>
    public static void Publish(float master, float sfx, float bgm, float ambience)
    {
        CEventBus<OnOptionVolumeChanged>.Publish(new OnOptionVolumeChanged(master, sfx, bgm, ambience));
    }
}
