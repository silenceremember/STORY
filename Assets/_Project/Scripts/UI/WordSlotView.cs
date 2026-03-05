using TMPro;
using UnityEngine;
using Story.Data;
using Story.Core;

namespace Story.UI
{
    /// <summary>
    /// Отображает один слот инвентаря слов.
    /// Пустой слот — GameObject выключен. Заполненный — TextButton с именем слова.
    /// </summary>
    public class WordSlotView : MonoBehaviour
    {
        [SerializeField] private TextButton     button;

        [Header("ScriptableObjects (назначает WordInventoryView)")]
        [HideInInspector] public GameStateSO     gameState;
        [HideInInspector] public WandererStatsSO stats;
        [HideInInspector] public WordInventorySO inventory;

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

            // Обновляем текст через TMP внутри TextButton
            var tmp = button != null
                ? button.GetComponentInChildren<TMP_Text>()
                : null;
            if (tmp != null) tmp.text = word.displayText;
        }

        private void OnClick()
        {
            if (_word == null) return;
            WordSpendProcessor.Spend(_word, inventory, gameState, stats);
        }
    }
}
