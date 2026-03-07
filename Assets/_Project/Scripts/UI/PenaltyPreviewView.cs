using TMPro;
using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Единый блок «шанс + штраф».
    /// Показывается и скрывается одним вызовом; все значения устанавливаются мгновенно.
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

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Показать блок и установить значения мгновенно.</summary>
        public void Show(float chance, int dHp, int dPow, int dSan)
        {
            gameObject.SetActive(true);
            SetChance(chance);
            SetPenalty(dHp, dPow, dSan);
        }

        /// <summary>Обновить шанс (число).</summary>
        public void UpdateChance(float chance) => SetChance(chance);

        /// <summary>Обновить шанс с rich-text (для hover-preview).</summary>
        public void UpdateChance(string richText)
        {
            if (chanceText == null) return;
            chanceText.text = richText;
        }

        /// <summary>Обновить penalty мгновенно.</summary>
        public void UpdatePenalty(int dHp, int dPow, int dSan) => SetPenalty(dHp, dPow, dSan);

        /// <summary>Скрыть весь блок.</summary>
        public void Hide() => gameObject.SetActive(false);

        // ── Internal ──────────────────────────────────────────────────────

        private void SetChance(float chance)
        {
            if (chanceText == null) return;
            chanceText.text = $"{Mathf.RoundToInt(chance * 100)}%";
        }

        private void SetPenalty(int dHp, int dPow, int dSan)
        {
            SetCell(hpText,  dHp);
            SetCell(powText, dPow);
            SetCell(sanText, dSan);
        }

        private void SetCell(TMP_Text txt, int value)
        {
            if (txt == null) return;

            Color c = style != null
                ? (value > 0 ? style.positiveColor
                 : value < 0 ? style.negativeColor
                 : style.neutralColor)
                : Color.white;

            txt.text  = value > 0 ? $"+{value}" : value.ToString();
            txt.color = c;
        }
    }
}
