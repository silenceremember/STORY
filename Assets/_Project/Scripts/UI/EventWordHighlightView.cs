using TMPro;
using UnityEngine;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Управляет подсветкой слов в тексте события при hover над слотом инвентаря.
    ///
    /// Фазы:
    ///   • Event-фаза:   текст события; hover → слово золотое; отпустить → обычный цвет
    ///   • Outcome-фаза: текст исхода; hover → временно показать event-текст с золотым словом;
    ///                   отпустить → вернуть текст исхода
    ///
    /// Вешается на тот же GameObject что и main TMP_Text / TypewriterEffect.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class EventWordHighlightView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private WordDatabaseSO     wordDatabase;

        // Обработанный текст события с [adj:key]/[noun:key] токенами
        private string _processedEventText;

        // Rich-text строка текущего outcome (чтобы восстанавливать при уходе курсора)
        private string _outcomeRichText;

        private TMP_Text _text;

        private void Awake()     => _text = GetComponent<TMP_Text>();
        private void OnEnable()  { if (hoverChannel) hoverChannel.OnHoverChanged += OnHover; }
        private void OnDisable() { if (hoverChannel) hoverChannel.OnHoverChanged -= OnHover; }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// GameManager вызывает перед PlayRichAsync события — сохраняет processed text.
        /// </summary>
        public void SetContent(string processedEventText)
        {
            _processedEventText = processedEventText;
        }

        /// <summary>
        /// GameManager вызывает после PlayRichAsync исхода — запоминаем rich-text
        /// чтобы уметь восстанавливать при hover.
        /// </summary>
        public void SetOutcomeContent(string richOutcomeText)
        {
            _outcomeRichText = richOutcomeText;
        }

        /// <summary>
        /// Сброс при уходе в следующий день (очищает и event, и outcome контент).
        /// </summary>
        public void ClearContent()
        {
            _processedEventText = null;
            _outcomeRichText    = null;
        }

        // ── Hover ─────────────────────────────────────────────────────────

        private void OnHover(WordSO hoveredWord)
        {
            if (string.IsNullOrEmpty(_processedEventText)) return;

            if (hoveredWord != null)
            {
                // Показываем event-текст с подсветкой нужного слова
                _text.text                 = OutcomeParser.ParseEventText(
                    _processedEventText, wordDatabase, hoveredWord);
                _text.maxVisibleCharacters = int.MaxValue;
            }
            else
            {
                // Курсор ушёл — восстанавливаем нужный текст
                if (!string.IsNullOrEmpty(_outcomeRichText))
                {
                    // Outcome-фаза: вернуть текст исхода
                    _text.text                 = _outcomeRichText;
                    _text.maxVisibleCharacters = int.MaxValue;
                }
                else
                {
                    // Event-фаза: вернуть event-текст без подсветки
                    _text.text                 = OutcomeParser.ParseEventText(
                        _processedEventText, wordDatabase, null);
                    _text.maxVisibleCharacters = int.MaxValue;
                }
            }
        }
    }
}
