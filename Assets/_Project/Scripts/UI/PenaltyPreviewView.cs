using TMPro;
using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Показывает 3 числа (HP / POW / SAN) разбитого penalty.
    /// Анимирует переход от текущего значения к новому (cancel-safe).
    /// Цвета и длительность задаются через PenaltyPreviewStyleSO.
    /// </summary>
    public class PenaltyPreviewView : MonoBehaviour
    {
        [SerializeField] private PenaltyPreviewStyleSO style;

        [Header("UI References")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text powText;
        [SerializeField] private TMP_Text sanText;

        // Current displayed (fractional) values for smooth lerp
        private float _dispHp, _dispPow, _dispSan;
        // Target values
        private int _targetHp, _targetPow, _targetSan;
        // Animation state
        private float _animTimer;
        private float _startHp, _startPow, _startSan;
        private bool  _animating;

        private float AnimDuration => style != null ? style.animDuration : 0.4f;

        /// <summary>Show and animate from 0 → targets.</summary>
        public void ShowFrom(int dHp, int dPow, int dSan)
        {
            gameObject.SetActive(true);
            _dispHp = 0; _dispPow = 0; _dispSan = 0;
            SetTargets(dHp, dPow, dSan);
        }

        /// <summary>Animate from current displayed values to new targets.</summary>
        public void SetTargets(int dHp, int dPow, int dSan)
        {
            _targetHp  = dHp;
            _targetPow = dPow;
            _targetSan = dSan;

            _startHp  = _dispHp;
            _startPow = _dispPow;
            _startSan = _dispSan;

            _animTimer = 0f;
            _animating = true;
        }

        /// <summary>Snap to values without animation.</summary>
        public void SetImmediate(int dHp, int dPow, int dSan)
        {
            gameObject.SetActive(true);
            _targetHp  = dHp;  _dispHp  = dHp;
            _targetPow = dPow; _dispPow = dPow;
            _targetSan = dSan; _dispSan = dSan;
            _animating = false;
            Render();
        }

        public void Hide()
        {
            _animating = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_animating) return;

            _animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer / AnimDuration);
            // EaseOutQuad
            t = 1f - (1f - t) * (1f - t);

            _dispHp  = Mathf.Lerp(_startHp,  _targetHp,  t);
            _dispPow = Mathf.Lerp(_startPow, _targetPow, t);
            _dispSan = Mathf.Lerp(_startSan, _targetSan, t);

            Render();

            if (t >= 1f) _animating = false;
        }

        private void Render()
        {
            SetCell(hpText,  _dispHp);
            SetCell(powText, _dispPow);
            SetCell(sanText, _dispSan);
        }

        private void SetCell(TMP_Text txt, float value)
        {
            if (txt == null) return;

            int rounded = Mathf.RoundToInt(value);
            Color c = style != null
                ? (rounded > 0 ? style.positiveColor
                 : rounded < 0 ? style.negativeColor
                 : style.neutralColor)
                : Color.white;

            if (rounded > 0)
                txt.text = $"+{rounded}";
            else
                txt.text = rounded.ToString();

            txt.color = c;
        }
    }
}
