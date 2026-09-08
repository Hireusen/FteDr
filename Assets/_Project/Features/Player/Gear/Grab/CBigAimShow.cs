using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
namespace Project
{
    public class CBigAimShow : AMono
    {
        #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
        [SerializeField] private Image _normal;
        [SerializeField] private Image _reached;
        [SerializeField] private Image _notreached;
        #endregion

        #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

        #endregion

        #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
        public enum EAimtype{
            normal,
            reached,
            notreached,
        }
        public void ShowTypeAim(EAimtype type)
        {
            _normal.gameObject.SetActive(false);
            _reached.gameObject.SetActive(false);
            _notreached.gameObject.SetActive(false);

            switch (type)
            {
                case EAimtype.normal:
                    _normal.gameObject.SetActive(true);
                    break;
                case EAimtype.reached:
                    _reached.gameObject.SetActive(true);
                    break;
                case EAimtype.notreached:
                    _notreached.gameObject.SetActive(true);
                    break;
            }

        }
        #endregion

        #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

        #endregion

        #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────

        #endregion

        #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

        #endregion
    }
}
