using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Отображает один слот инвентаря слов.
    /// Пустой слот — GameObject выключен.
    /// Заполненный — TextButton с именем слова + hover-уведомление через HoverWordChannelSO.
    /// </summary>
    public class WordSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextButton     button;

        [Header("ScriptableObjects (назначает WordInventoryView)")]
        [HideInInspector] public GameStateSO     gameState;
        [HideInInspector] public WandererStatsSO stats;
        [HideInInspector] public WordInventorySO inventory;
        [HideInInspector] public HoverWordChannelSO hoverChannel;

        private WordSO _word;

        private void Awake()
        {
            if (button != null)
                button.OnClick += OnClick;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.OnClick -= OnClick;
        }

        /// <summary>
        /// Назначает слово. null — слот скрывается (SetActive false).
        /// </summary>
        public void SetWord(WordSO word)
        {
            _word = word;

            if (word == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            var tmp = button != null
                ? button.GetComponentInChildren<TMP_Text>()
                : null;
            if (tmp != null) tmp.text = word.displayText.ToUpperInvariant();
        }

        // ── Pointer events ────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData _)
        {
            if (_word != null)
                hoverChannel?.SetHovered(_word);
        }

        public void OnPointerExit(PointerEventData _)
        {
            hoverChannel?.SetHovered(null);
        }

        // ── Click ─────────────────────────────────────────────────────────

        private void OnClick()
        {
            if (_word == null) return;
            WordSpendProcessor.Spend(_word, inventory, gameState, stats);
        }
    }
}
