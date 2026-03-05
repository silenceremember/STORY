using TMPro;
using UnityEngine;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Управляет отображением составной фразы и подсветкой.
    ///
    /// Фазы:
    ///   Event  — показывает составную фразу (intent+action), обновляет при hover/click
    ///   Outcome — показывает outcome rich-text, блокирует инвентарь
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class EventWordHighlightView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private WordDatabaseSO     wordDatabase;
        [SerializeField] private WordInventorySO    wordInventory;

        [Header("Typewriter для фразы (отдельный от main)")]
        [SerializeField] private TypewriterEffect phraseTypewriter;

        private EventSO         _currentEvent;
        private string          _outcomeRichText;
        private bool            _isOutcomePhase;

        /// <summary>true во время outcome — слоты инвентаря неактивны.</summary>
        public bool IsOutcomePhase => _isOutcomePhase;

        private TMP_Text _text;

        private void Awake()     => _text = GetComponent<TMP_Text>();
        private void OnEnable()
        {
            if (hoverChannel)  hoverChannel.OnHoverChanged  += OnHover;
            if (wordInventory) wordInventory.OnChanged       += OnInventoryChanged;
        }
        private void OnDisable()
        {
            if (hoverChannel)  hoverChannel.OnHoverChanged  -= OnHover;
            if (wordInventory) wordInventory.OnChanged       -= OnInventoryChanged;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Вызывается при начале event-фазы.</summary>
        public void SetEventPhase(EventSO ev, WordInventorySO inventory)
        {
            _currentEvent    = ev;
            _outcomeRichText = null;
            _isOutcomePhase  = false;
        }

        /// <summary>Вызывается при переходе к outcome.</summary>
        public void SetOutcomePhase(string richOutcomeText)
        {
            _outcomeRichText = richOutcomeText;
            _isOutcomePhase  = true;
        }

        public void ClearContent()
        {
            _currentEvent    = null;
            _outcomeRichText = null;
            _isOutcomePhase  = false;
        }

        /// <summary>
        /// Всегда true в event-фазе — любое слово может быть использовано.
        /// </summary>
        public bool HasSlotForWord(WordSO word)
            => word != null && _currentEvent != null && !_isOutcomePhase;

        // ── Hover ─────────────────────────────────────────────────────────

        private void OnHover(WordSO hoveredWord)
        {
            if (_currentEvent == null || _isOutcomePhase) return;

            // Превью фразы при наведении
            if (hoveredWord != null)
                UpdatePhrasePreview(hoveredWord);
            else
                RefreshPhrase();
        }

        // ── Internal ──────────────────────────────────────────────────────

        /// <summary>
        /// Обновляет фразу при изменении инвентаря (клик = активация/деактивация).
        /// </summary>
        private void OnInventoryChanged()
        {
            if (_currentEvent == null || _isOutcomePhase) return;
            RefreshPhrase();
        }

        /// <summary>Показывает фразу с текущими активными словами.</summary>
        private void RefreshPhrase()
        {
            if (_currentEvent == null) return;
            string phrase = _currentEvent.BuildPhrase(wordInventory);
            SetPhraseText(phrase);
        }

        /// <summary>Показывает превью фразы с hovered словом.</summary>
        private void UpdatePhrasePreview(WordSO hoveredWord)
        {
            if (_currentEvent == null) return;

            string start = _currentEvent.defaultPhraseStart;
            string end   = _currentEvent.defaultPhraseEnd;

            // Активные слова
            if (wordInventory != null)
            {
                var activeAdj = wordInventory.GetActive(WordType.Adjective);
                if (activeAdj != null && !string.IsNullOrEmpty(activeAdj.phraseStart))
                    start = activeAdj.phraseStart;

                var activeNoun = wordInventory.GetActive(WordType.Noun);
                if (activeNoun != null && !string.IsNullOrEmpty(activeNoun.phraseEnd))
                    end = activeNoun.phraseEnd;
            }

            // Hovered слово заменяет свой тип
            if (hoveredWord.type == WordType.Adjective && !string.IsNullOrEmpty(hoveredWord.phraseStart))
                start = $"<color={OutcomeParser.ColorGold}>{hoveredWord.phraseStart}</color>";
            else if (hoveredWord.type == WordType.Noun && !string.IsNullOrEmpty(hoveredWord.phraseEnd))
                end = $"<color={OutcomeParser.ColorGold}>{hoveredWord.phraseEnd}</color>";

            SetPhraseText($"{start} {end}");
        }

        private void SetPhraseText(string phrase)
        {
            if (phraseTypewriter != null && phraseTypewriter.TextComponent != null)
            {
                var tmp = phraseTypewriter.TextComponent;
                tmp.text = phrase.ToUpperInvariant();
                tmp.maxVisibleCharacters = int.MaxValue;
            }
        }
    }
}
