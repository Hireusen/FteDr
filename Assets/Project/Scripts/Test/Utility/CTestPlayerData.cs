using UnityEngine;

/// <summary>
/// 플레이어 데이터(산소/가방/돈/업그레이드)를 인스펙터에서 테스트하는 스크립트입니다.
/// 컴포넌트 우측 상단 ⋮ 메뉴(또는 우클릭)의 항목을 눌러 실행합니다.
/// 개발용이며, 빌드에서는 제거하거나 비활성화하세요.
///
/// 사용법: 수치는 아래 필드에서 조절하고, 실행은 컨텍스트 메뉴 버튼으로 합니다.
/// </summary>
public sealed class CTestPlayerData : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
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

    #region ─────────────────────────▶ 산소 ◀─────────────────────────
    [ContextMenu("산소/소모")]
    private void ConsumeOxygen()
    {
        UPlayer.ConsumeOxygen(_oxygenAmount);
        UDebug.Print($"[Test] 산소 소모 {_oxygenAmount} → {UPlayer.CurrentOxygen:F1}/{UPlayer.MaxOxygen:F1}");
    }

    [ContextMenu("산소/회복")]
    private void RecoverOxygen()
    {
        UPlayer.RecoverOxygen(_oxygenAmount);
        UDebug.Print($"[Test] 산소 회복 {_oxygenAmount} → {UPlayer.CurrentOxygen:F1}/{UPlayer.MaxOxygen:F1}");
    }

    [ContextMenu("산소/최대치 패널티 적용")]
    private void ApplyOxygenPenalty()
    {
        UPlayer.ApplyOxygenPenalty(_oxygenAmount);
        UDebug.Print($"[Test] 최대 산소 패널티 {_oxygenAmount} → 최대 {UPlayer.MaxOxygen:F1}");
    }

    [ContextMenu("산소/새 잠수 시작(ResetForNew)")]
    private void ResetForNew()
    {
        UPlayer.ResetForNew();
        UDebug.Print($"[Test] 새 잠수 시작 → {UPlayer.CurrentOxygen:F1}/{UPlayer.MaxOxygen:F1}");
    }
    #endregion

    #region ─────────────────────────▶ 소지품 ◀─────────────────────────
    [ContextMenu("소지품/가방에 일반 수집품 추가")]
    private void AddToBag()
    {
        bool ok = UPlayer.TryAddToBag(_testCollectibleId);
        UDebug.Print($"[Test] 가방 담기 {(ok ? "성공" : "실패(가득참)")} — 현재 용량 {UPlayer.BagCapacity}");
    }

    [ContextMenu("소지품/특수 수집품 손에 들기")]
    private void HoldSpecial()
    {
        bool ok = UPlayer.TryHoldSpecial(_testSpecialId);
        UDebug.Print($"[Test] 특수 들기 {(ok ? "성공" : "실패(이미 들고있음)")}");
    }
    #endregion

    #region ─────────────────────────▶ 재화 ◀─────────────────────────
    [ContextMenu("재화/돈 추가")]
    private void AddMoney()
    {
        UPlayer.AddMoney(_moneyAmount);
        UDebug.Print($"[Test] 돈 +{_moneyAmount} → {UPlayer.Money}");
    }

    [ContextMenu("재화/돈 차감")]
    private void SpendMoney()
    {
        bool ok = UPlayer.TrySpendMoney(_moneyAmount);
        UDebug.Print($"[Test] 돈 -{_moneyAmount} {(ok ? "성공" : "실패(부족)")} → {UPlayer.Money}");
    }
    #endregion

    #region ─────────────────────────▶ 업그레이드 ◀─────────────────────────
    [ContextMenu("업그레이드/대상 장비 1레벨 올리기")]
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
