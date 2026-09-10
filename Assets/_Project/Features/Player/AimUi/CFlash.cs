using Codice.CM.Common;
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
        [SerializeField] private Image _screenImg;
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
        public void ScreenOn(bool onScreen)
        {
            StopAllCoroutines();
            if (onScreen)
            {
                
                StartCoroutine(ScreenOnCo(0.5f, 1f, 5f));
            }
            else
            {
                StartCoroutine(ScreenOnCo(1f, 0.5f, 5f));
            }
        }
        private IEnumerator ScreenOnCo(float startScale,float targetXScale,float speed)
        {
            Vector3 tempScale = _screenImg.transform.localScale;
            tempScale.x = startScale;
            _screenImg.transform.localScale = tempScale;
            // 목표값과 현재값의 차이가 아주 작아질 때까지 반복
            while (Mathf.Abs(targetXScale - _screenImg.transform.localScale.x) > 0.01f)
            {
                Vector3 currentScale = _screenImg.transform.localScale;

                // 매 프레임 현재 X값에서 목표 X값으로 부드럽게 보간
                float newX = Mathf.Lerp(currentScale.x, targetXScale, Time.deltaTime * speed);

                _screenImg.transform.localScale = new Vector3(newX, currentScale.y, currentScale.z);

                yield return null;
            }

            // 최종 값 맞추기
            _screenImg.transform.localScale = new Vector3(targetXScale, _screenImg.transform.localScale.y, _screenImg.transform.localScale.z);


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
