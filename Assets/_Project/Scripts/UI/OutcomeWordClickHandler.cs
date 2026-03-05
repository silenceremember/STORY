using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Добавьте на тот же GameObject что и mainTypewriter (TMP_Text).
    /// Слушает клики через IPointerClickHandler (совместимо с новым Input System)
    /// и при попадании в TMP-ссылку добавляет слово в инвентарь.
    /// </summary>
    public class OutcomeWordClickHandler : MonoBehaviour, IPointerClickHandler
    {
        [Header("ScriptableObjects")]
        [SerializeField] private WordDatabaseSO  wordDatabase;
        [SerializeField] private WordInventorySO wordInventory;

        [Header("TypewriterEffect на этом же объекте")]
        [SerializeField] private TypewriterEffect typewriter;

        private string   _rawText;
        private bool     _active;

        private TMP_Text TmpText => typewriter != null ? typewriter.TextComponent : null;

        // ── Public API (вызывается из GameManager) ────────────────────────

        public void Activate(string processedOutcomeText)
        {
            _rawText = processedOutcomeText;
            _active  = true;
        }

        public void Deactivate()
        {
            _active  = false;
            _rawText = null;
        }

        // ── IPointerClickHandler ──────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_active) return;
            var tmp = TmpText;
            if (tmp == null) return;

            // Находим TMP-ссылку под позицией клика
            int linkIdx = TMP_TextUtilities.FindIntersectingLink(
                tmp, eventData.position, eventData.pressEventCamera);

            if (linkIdx < 0) return;

            string key  = tmp.textInfo.linkInfo[linkIdx].GetLinkID();
            var    word = wordDatabase?.GetByKey(key);
            if (word == null) return;

            if (wordInventory != null && wordInventory.TryAdd(word))
                RefreshRichText();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void RefreshRichText()
        {
            var tmp = TmpText;
            if (tmp == null) return;

            string rich = OutcomeParser.Parse(_rawText, wordDatabase, wordInventory);
            tmp.text               = rich;
            tmp.maxVisibleCharacters = int.MaxValue;
        }
    }
}
