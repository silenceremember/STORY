using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Управляет двумя панелями слотов (прилагательные и существительные).
    /// Подписывается на WordInventorySO.OnChanged и перерисовывает слоты.
    /// </summary>
    public class WordInventoryView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private WordInventorySO    inventory;
        [SerializeField] private GameStateSO        gameState;
        [SerializeField] private WandererStatsSO    stats;
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private EventWordHighlightView eventHighlight;

        [Header("6 слотов — прилагательные (слева)")]
        [SerializeField] private WordSlotView[] adjectiveSlots = new WordSlotView[6];

        [Header("6 слотов — существительные (справа)")]
        [SerializeField] private WordSlotView[] nounSlots = new WordSlotView[6];

        private void Awake()
        {
            // Прокидываем зависимости в каждый слот
            InitSlots(adjectiveSlots);
            InitSlots(nounSlots);
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnChanged += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnChanged -= Refresh;
        }

        private void InitSlots(WordSlotView[] slots)
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.inventory      = inventory;
                slot.gameState      = gameState;
                slot.stats          = stats;
                slot.hoverChannel   = hoverChannel;
                slot.eventHighlight = eventHighlight;
            }
        }

        private void Refresh()
        {
            if (inventory == null) return;

            RefreshSlots(adjectiveSlots, inventory.adjectives);
            RefreshSlots(nounSlots,      inventory.nouns);
        }

        private static void RefreshSlots(WordSlotView[] views,
            System.Collections.Generic.List<WordSO> words)
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null) continue;
                views[i].SetWord(i < words.Count ? words[i] : null);
            }
        }
    }
}
