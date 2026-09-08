using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
namespace Project
{
    public class CFlash : AMono
    {
        #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
        [SerializeField] private Image _flashImg;
        #endregion

        #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

        #endregion

        #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
        public void FlashShow(float startduratioin,float endduration)
        {
            StartCoroutine(FlashCo(startduratioin, endduration));
        }

        #endregion

        #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
        private IEnumerator FlashCo(float startduration,float endduration)
        {

            // 빠르게 밝아짐
            for (float t = 0; t < startduration; t += Time.deltaTime)
            {
                float normalized = t / startduration;
                _flashImg.color = new Color(1, 1, 1, normalized);
                yield return null;
            }

            // 빠르게 어두워짐
            for (float t = 0; t < endduration; t += Time.deltaTime)
            {
                float normalized = 1f - t / endduration;
                _flashImg.color = new Color(1, 1, 1, normalized);
                yield return null;
            }

            _flashImg.color = new Color(1, 1, 1, 0);
        }
        #endregion

        #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
        private void Awake()
        {
            _flashImg.color = new Color(1, 1, 1, 0);
        }
        #endregion

        #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

        #endregion
    }
}
