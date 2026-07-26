using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "기본은 자동, 특정 버튼만 수동 제외" 정책의 버튼 인터랙션(호버 스케일 + 클릭 펀치 + 클릭 SFX) 자동 장착 유틸리티입니다.
/// 특정 뿌리(예: 창 하나, HUD 하나)의 자식 버튼들을 한 번에 훑어서 CScaleResponsiveButton을 붙여줍니다.
///
/// 씬 전체를 매번 스캔하지 않고, CUIWindow/HUD/타이틀처럼 "버튼이 실제로 속한 뿌리"에서
/// 자기 자신이 처음 활성화될 때 딱 한 번만 호출하는 방식이라 가볍습니다.
/// </summary>
public static class UButtonFx
{
    /// <summary>
    /// root 아래(비활성 자식 포함)의 모든 Button에 CScaleResponsiveButton을 자동으로 붙입니다.
    /// 이미 붙어있거나 CButtonFxExclude가 있는 버튼은 건너뜁니다.
    /// </summary>
    /// <param name="root">스캔할 뿌리 오브젝트</param>
    /// <param name="hoverScaleFactor">호버 시 커질 배율</param>
    /// <param name="transitionDuration">크기 변화 시간(초)</param>
    /// <param name="clickSfxId">클릭 시 재생할 사운드 ID. null/빈 문자열이면 무음</param>
    public static void AutoEquip(GameObject root, float hoverScaleFactor = 1.08f, float transitionDuration = 0.15f, string clickSfxId = null)
    {
        if (root == null) return;

        Button[] buttons = root.GetComponentsInChildren<Button>(includeInactive: true);
        int count = buttons.Length;
        for (int i = 0; i < count; ++i)
        {
            Button button = buttons[i];
            if (button == null) continue;

            GameObject buttonObj = button.gameObject;
            if (buttonObj.GetComponent<CButtonFxExclude>() != null) continue; // 수동 제외

            // 이미 자체 Animator Controller로 호버/클릭 연출을 갖고 있는 버튼(디자이너 제작 애니메이션)은
            // 코드 스케일 연출을 얹지 않는다. 대신 클릭 SFX만 별도로 붙인다.
            Animator animator = buttonObj.GetComponent<Animator>();
            bool hasCustomAnimator = animator != null && animator.runtimeAnimatorController != null;

            if (hasCustomAnimator)
            {
                if (buttonObj.GetComponent<CButtonPunchAnimator>() != null) continue; // 이미 있음
                CButtonPunchAnimator punch = buttonObj.AddComponent<CButtonPunchAnimator>();
                punch.Initialize(clickSfxId);
                continue;
            }

            if (buttonObj.GetComponent<CScaleResponsiveButton>() != null) continue; // 이미 있음 (중복 방지)

            CScaleResponsiveButton fx = buttonObj.AddComponent<CScaleResponsiveButton>();
            fx.Initialize(hoverScaleFactor, transitionDuration, clickSfxId);
        }
    }
}
