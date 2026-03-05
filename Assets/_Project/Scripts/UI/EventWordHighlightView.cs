using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Управляет фазами: Event vs Outcome.
    ///
    /// В Event-фазе — инвентарь активен.
    /// В Outcome-фазе — инвентарь заблокирован.
    ///
    /// Обновление фразы на кнопке и hover-preview делегировано GameManager
    /// (через WordInventorySO.OnChanged и HoverWordChannelSO.OnHoverChanged).
    /// </summary>
    public class EventWordHighlightView : MonoBehaviour
    {
        private EventSO _currentEvent;
        private bool    _isOutcomePhase;

        /// <summary>true во время outcome — слоты инвентаря неактивны.</summary>
        public bool IsOutcomePhase => _isOutcomePhase;

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
    }
}
