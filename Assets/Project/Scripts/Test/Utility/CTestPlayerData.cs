using UnityEngine;

/// <summary>
/// 플레이어 데이터(산소/가방/돈/업그레이드)를 인스펙터에서 테스트하는 스크립트입니다.
/// </summary>
public sealed class CTestPlayerData : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("테스트 트리거")]
    [SerializeField] private bool _testConsumeOxygen;
    [SerializeField] private bool _testRecoverOxygen;
    [SerializeField] private bool _testApplyOxygenPenalty;
    [SerializeField] private bool _testResetForNew;
    [SerializeField] private bool _testAddToBag;
    [SerializeField] private bool _testHoldSpecial;
    [SerializeField] private bool _testAddMoney;
    [SerializeField] private bool _testSpendMoney;
    [SerializeField] private bool _testUpgradeTarget;

    [Header("산소")]
    [SerializeField] private float _oxygenAmount = 10f;

    [Header("재화")]
    [SerializeField] private int _moneyAmount = 100;

    [Header("소지품")]
    [SerializeField] private string _testCollectibleId = "collectible_test";
    [SerializeField] private string _testSpecialId = "special_test";

    [Header("업그레이드 대상")]
    [SerializeField] private EDataType _upgradeTarget = EDataType.OxygenTank;

    [Header("표시")]
    [SerializeField] private bool _showOnGUI = true;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public void ExecuteUpdateFrame()
    {
        if (_testConsumeOxygen) { _testConsumeOxygen = false; ConsumeOxygen(); }
        if (_testRecoverOxygen) { _testRecoverOxygen = false; RecoverOxygen(); }
        if (_testApplyOxygenPenalty) { _testApplyOxygenPenalty = false; ApplyOxygenPenalty(); }
        if (_testResetForNew) { _testResetForNew = false; ResetForNew(); }
        if (_testAddToBag) { _testAddToBag = false; AddToBag(); }
        if (_testHoldSpecial) { _testHoldSpecial = false; HoldSpecial(); }
        if (_testAddMoney) { _testAddMoney = false; AddMoney(); }
        if (_testSpendMoney) { _testSpendMoney = false; SpendMoney(); }
        if (_testUpgradeTarget) { _testUpgradeTarget = false; UpgradeTarget(); }
    }

    private void ConsumeOxygen()
    {
        UPlayer.ConsumeOxygen(_oxygenAmount);
        UDebug.Print($"[Test] 산소 소모 {_oxygenAmount} → {UPlayer.CurrentOxygen:F1}/{UPlayer.MaxOxygen:F1}");
    }

    private void RecoverOxygen()
    {
        UPlayer.RecoverOxygen(_oxygenAmount);
        UDebug.Print($"[Test] 산소 회복 {_oxygenAmount} → {UPlayer.CurrentOxygen:F1}/{UPlayer.MaxOxygen:F1}");
    }

    private void ApplyOxygenPenalty()
    {
        UPlayer.ApplyOxygenPenalty(_oxygenAmount);
        UDebug.Print($"[Test] 최대 산소 패널티 {_oxygenAmount} → 최대 {UPlayer.MaxOxygen:F1}");
    }

    private void ResetForNew()
    {
        UPlayer.ResetForNew();
        UDebug.Print($"[Test] 새 잠수 시작 → {UPlayer.CurrentOxygen:F1}/{UPlayer.MaxOxygen:F1}");
    }

    private void AddToBag()
    {
        bool ok = UPlayer.TryAddToBag(_testCollectibleId);
        UDebug.Print($"[Test] 가방 담기 {(ok ? "성공" : "실패(가득참)")} — 현재 용량 {UPlayer.BagCapacity}");
    }

    private void HoldSpecial()
    {
        bool ok = UPlayer.TryHoldSpecial(_testSpecialId);
        UDebug.Print($"[Test] 특수 들기 {(ok ? "성공" : "실패(이미 들고있음)")}");
    }

    private void AddMoney()
    {
        UPlayer.AddMoney(_moneyAmount);
        UDebug.Print($"[Test] 돈 +{_moneyAmount} → {UPlayer.Money}");
    }

    private void SpendMoney()
    {
        bool ok = UPlayer.TrySpendMoney(_moneyAmount);
        UDebug.Print($"[Test] 돈 -{_moneyAmount} {(ok ? "성공" : "실패(부족)")} → {UPlayer.Money}");
    }

    private void UpgradeTarget()
    {
        bool ok = UPlayer.UpgradeGear(_upgradeTarget);
        int level = UPlayer.GetGearLevel(_upgradeTarget);
        UDebug.Print($"[Test] {_upgradeTarget} 업그레이드 {(ok ? "성공" : "실패(최대 레벨 등)")} → Lv.{level}");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnGUI()
    {
        if (!_showOnGUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 200), GUI.skin.box);
        GUILayout.Label("<b>[ 플레이어 데이터 ]</b>");
        GUILayout.Label($"산소: {UPlayer.CurrentOxygen:F1} / {UPlayer.MaxOxygen:F1}  {(UPlayer.IsOxygenLow ? "⚠ 부족" : "")}");
        GUILayout.Label($"돈: {UPlayer.Money}");
        GUILayout.Label($"산소Lv {UPlayer.GetGearLevel(EDataType.OxygenTank)} / 레이더Lv {UPlayer.GetGearLevel(EDataType.Radar)}");
        GUILayout.Label($"가방Lv {UPlayer.GetGearLevel(EDataType.Bag)} / 추진Lv {UPlayer.GetGearLevel(EDataType.Thruster)} / 집게Lv {UPlayer.GetGearLevel(EDataType.GrabTool)}");
        GUILayout.EndArea();
    }
    #endregion
}
