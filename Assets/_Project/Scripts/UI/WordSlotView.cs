using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Отображает один слот инвентаря слов.
    ///
    /// Состояния:
    ///   • Обычное  → белый
    ///   • Активное → серый
    ///   • Outcome-фаза → клик/hover заблокированы
    ///
    /// Клик = ToggleActive (слово остаётся в инвентаре, меняет фразу ответа).
    /// </summary>
    public class WordSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextButton button;

        [Header("ScriptableObjects (назначает WordInventoryView)")]
        [HideInInspector] public WordInventorySO        inventory;
        [HideInInspector] public HoverWordChannelSO     hoverChannel;
        [HideInInspector] public EventWordHighlightView eventHighlight;

        private WordSO   _word;
        private TMP_Text _tmp;

        private void Awake()
        {
            _tmp = button != null ? button.GetComponentInChildren<TMP_Text>() : null;

            if (button != null)
                button.OnClick += OnClick;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.OnClick -= OnClick;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void SetWord(WordSO word)
        {
            _word = word;

            if (word == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            RefreshDisplay();
        }

        // ── Pointer events ────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData _)
        {
            if (_word == null) return;

            // В outcome-фазе — ничего не делаем
            if (eventHighlight != null && eventHighlight.IsOutcomePhase) return;

            hoverChannel?.SetHovered(_word);
        }

        public void OnPointerExit(PointerEventData _)
        {
            hoverChannel?.SetHovered(null);
            RefreshDisplay();
        }

        // ── Click ─────────────────────────────────────────────────────────

        private void OnClick()
        {
            if (_word == null) return;

            // В outcome-фазе — клик игнорируется
            if (eventHighlight != null && eventHighlight.IsOutcomePhase) return;

            inventory?.ToggleActive(_word);
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            if (_tmp == null || _word == null) return;

            bool isActive = inventory != null && inventory.IsActive(_word);
            _tmp.text = isActive
                ? $"<color={OutcomeParser.ColorGray}>{_word.displayText.ToUpperInvariant()}</color>"
                : _word.displayText.ToUpperInvariant();
        }
    }
}
