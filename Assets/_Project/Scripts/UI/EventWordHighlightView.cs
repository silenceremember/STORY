using TMPro;
using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Управляет фазами: Event vs Outcome.
    ///
    /// В Event-фазе — инвентарь активен, hover подсвечивает слова.
    /// В Outcome-фазе — инвентарь заблокирован.
    ///
    /// Обновление фразы на кнопке действия делегировано GameManager
    /// (через WordInventorySO.OnChanged).
    /// </summary>
    public class EventWordHighlightView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private WordInventorySO    wordInventory;

        private EventSO _currentEvent;
        private bool    _isOutcomePhase;

        /// <summary>true во время outcome — слоты инвентаря неактивны.</summary>
        public bool IsOutcomePhase => _isOutcomePhase;

        private void OnEnable()
        {
            if (hoverChannel)  hoverChannel.OnHoverChanged += OnHover;
        }
        private void OnDisable()
        {
            if (hoverChannel)  hoverChannel.OnHoverChanged -= OnHover;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Вызывается при начале event-фазы.</summary>
        public void SetEventPhase(EventSO ev, WordInventorySO inventory)
        {
            _currentEvent   = ev;
            _isOutcomePhase = false;
        }

        /// <summary>Вызывается при переходе к outcome.</summary>
        public void SetOutcomePhase(string richOutcomeText)
        {
            _isOutcomePhase = true;
        }

        public void ClearContent()
        {
            _currentEvent   = null;
            _isOutcomePhase = false;
        }

        /// <summary>
        /// Всегда true в event-фазе — любое слово может быть использовано.
        /// </summary>
        public bool HasSlotForWord(WordSO word)
            => word != null && _currentEvent != null && !_isOutcomePhase;

        // ── Hover ─────────────────────────────────────────────────────────

        private void OnHover(WordSO hoveredWord)
        {
            // Hover-логика теперь минимальна — превью фразы обновляется
            // через GameManager.OnInventoryChangedUpdatePhrase
        }
    }
}
