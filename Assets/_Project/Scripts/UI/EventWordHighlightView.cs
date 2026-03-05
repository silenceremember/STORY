using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Управляет подсветкой слов в тексте события при hover над слотом инвентаря.
    ///
    /// Правила подсветки в event-тексте:
    ///   • Активное слово (inventory.IsActive)  → золотое (постоянно)
    ///   • Hovered слово с доступным слотом     → золотое
    ///   • Hover без слота в тексте             → текст не меняется
    ///
    /// Правила для инвентарного слота (сигнализирует через HasSlotForWord):
    ///   • Слот ЕСТЬ в тексте события  → WordSlotView показывает нормальный/серый цвет
    ///   • Слота НЕТ в тексте события  → WordSlotView показывает красный
    ///
    /// Также хранит richOutcomeText для восстановления при hover в фазе outcome.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class EventWordHighlightView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private WordDatabaseSO     wordDatabase;
        [SerializeField] private WordInventorySO    wordInventory;

        [Header("Typewriter (на том же объекте)")]
        [SerializeField] private TypewriterEffect typewriter;

        private string         _processedEventText;
        private string         _outcomeRichText;
        private HashSet<string> _wordsInText = new();

        private TMP_Text _text;

        private void Awake()     => _text = GetComponent<TMP_Text>();
        private void OnEnable()
        {
            if (hoverChannel)  hoverChannel.OnHoverChanged  += OnHover;
            if (wordInventory) wordInventory.OnChanged       += RefreshCurrentText;
        }
        private void OnDisable()
        {
            if (hoverChannel)  hoverChannel.OnHoverChanged  -= OnHover;
            if (wordInventory) wordInventory.OnChanged       -= RefreshCurrentText;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void SetContent(string processedEventText)
        {
            _processedEventText = processedEventText;
            _wordsInText        = OutcomeParser.GetKeywordsInText(processedEventText);
        }

        public void SetOutcomeContent(string richOutcomeText)
        {
            _outcomeRichText = richOutcomeText;
        }

        public void ClearContent()
        {
            _processedEventText = null;
            _outcomeRichText    = null;
            _wordsInText        = new HashSet<string>();
        }

        /// <summary>
        /// Возвращает true если в тексте события есть слот для данного слова.
        /// WordSlotView использует это чтобы решить — показывать ли красный цвет.
        /// </summary>
        public bool HasSlotForWord(WordSO word)
            => word != null && _wordsInText.Contains(word.key);

        // ── Hover ─────────────────────────────────────────────────────────

        private void OnHover(WordSO hoveredWord)
        {
            if (string.IsNullOrEmpty(_processedEventText)) return;

            // Не мешаем тайпрайтеру пока идёт анимация
            if (typewriter != null && typewriter.IsRunning) return;

            // В фазе outcome текст не меняем
            if (!string.IsNullOrEmpty(_outcomeRichText)) return;

            if (hoveredWord != null)
            {
                _text.text                 = OutcomeParser.ParseEventText(
                    _processedEventText, wordDatabase, hoveredWord, wordInventory);
                _text.maxVisibleCharacters = int.MaxValue;
            }
            else
            {
                RefreshCurrentText();
            }
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void RefreshCurrentText()
        {
            if (string.IsNullOrEmpty(_processedEventText)) return;

            // Не мешаем тайпрайтеру
            if (typewriter != null && typewriter.IsRunning) return;

            if (!string.IsNullOrEmpty(_outcomeRichText))
            {
                _text.text                 = _outcomeRichText;
                _text.maxVisibleCharacters = int.MaxValue;
                return;
            }

            _text.text                 = OutcomeParser.ParseEventText(
                _processedEventText, wordDatabase, null, wordInventory);
            _text.maxVisibleCharacters = int.MaxValue;
        }
    }
}
