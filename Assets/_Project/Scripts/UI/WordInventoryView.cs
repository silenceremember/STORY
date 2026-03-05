using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Управляет двумя панелями слотов (глаголы и существительные).
    /// Подписывается на WordInventorySO.OnChanged и перерисовывает слоты.
    /// </summary>
    public class WordInventoryView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private WordInventorySO    inventory;
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private EventWordHighlightView eventHighlight;

        [Header("6 слотов — глаголы (слева)")]
        [SerializeField] private WordSlotView[] verbSlots = new WordSlotView[6];

        [Header("6 слотов — существительные (справа)")]
        [SerializeField] private WordSlotView[] nounSlots = new WordSlotView[6];

        private void Awake()
        {
            InitSlots(verbSlots);
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
                slot.hoverChannel   = hoverChannel;
                slot.eventHighlight = eventHighlight;
            }
        }

        private void Refresh()
        {
            if (inventory == null) return;

            RefreshSlots(verbSlots, inventory.verbs);
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
