using TMPro;
using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Единый блок «шанс + штраф».
    /// Показывается и скрывается одним вызовом; шанс и penalty всегда идут вместе.
    /// </summary>
    public class PenaltyPreviewView : MonoBehaviour
    {
        [SerializeField] private PenaltyPreviewStyleSO style;

        [Header("Chance")]
        [SerializeField] private TMP_Text chanceText;

        [Header("Penalty (HP / POW / SAN)")]
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

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Показать блок и анимировать penalty 0 → targets. Шанс устанавливается сразу.</summary>
        public void Show(float chance, int dHp, int dPow, int dSan)
        {
            gameObject.SetActive(true);
            SetChance(chance);
            _dispHp = 0; _dispPow = 0; _dispSan = 0;
            AnimateTo(dHp, dPow, dSan);
        }

        /// <summary>Обновить шанс (текст, цвет).</summary>
        public void UpdateChance(float chance) => SetChance(chance);

        /// <summary>Обновить шанс с rich-text (для hover-preview).</summary>
        public void UpdateChance(string richText)
        {
            if (chanceText == null) return;
            chanceText.text = richText;
        }

        /// <summary>Обновить penalty (анимированно от текущего к новому).</summary>
        public void UpdatePenalty(int dHp, int dPow, int dSan) => AnimateTo(dHp, dPow, dSan);

        /// <summary>Скрыть весь блок.</summary>
        public void Hide()
        {
            _animating = false;
            gameObject.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void SetChance(float chance)
        {
            if (chanceText == null) return;
            chanceText.text = $"{Mathf.RoundToInt(chance * 100)}%";
        }

        private void AnimateTo(int dHp, int dPow, int dSan)
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

            txt.text  = rounded > 0 ? $"+{rounded}" : rounded.ToString();
            txt.color = c;
        }
    }
}
