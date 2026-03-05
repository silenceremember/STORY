using TMPro;
using UnityEngine;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Управляет подсветкой слов в тексте события при hover над слотом инвентаря.
    ///
    /// Вешается на тот же GameObject что и main TMP_Text.
    /// GameManager вызывает SetContent() перед PlayRichAsync,
    /// после чего компонент самостоятельно перекрашивает нужное слово при hover.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class EventWordHighlightView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private WordDatabaseSO     wordDatabase;

        // Обработанный текст с [adj:key]/[noun:key] токенами — для перерасчёта цветов
        private string   _processedText;
        private TMP_Text _text;

        private void Awake()       => _text = GetComponent<TMP_Text>();
        private void OnEnable()    { if (hoverChannel) hoverChannel.OnHoverChanged += OnHover; }
        private void OnDisable()   { if (hoverChannel) hoverChannel.OnHoverChanged -= OnHover; }

        /// <summary>
        /// GameManager вызывает после PreProcess и до PlayRichAsync.
        /// </summary>
        public void SetContent(string processedText)
        {
            _processedText = processedText;
        }

        /// <summary>
        /// Очистить контент (при переходе на следующий день).
        /// </summary>
        public void ClearContent() => _processedText = null;

        // ── Hover ─────────────────────────────────────────────────────────

        private void OnHover(WordSO hoveredWord)
        {
            if (string.IsNullOrEmpty(_processedText)) return;
            _text.text = OutcomeParser.ParseEventText(_processedText, wordDatabase, hoveredWord);
            _text.maxVisibleCharacters = int.MaxValue;
        }
    }
}
