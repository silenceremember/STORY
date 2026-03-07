using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Управляет двумя панелями слотов (подходы и опоры).
    /// Подписывается на WordInventorySO.OnChanged и перерисовывает слоты.
    /// </summary>
    public class WordInventoryView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private WordInventorySO    inventory;
        [SerializeField] private HoverWordChannelSO hoverChannel;
        [SerializeField] private EventWordHighlightView eventHighlight;

        [Header("Подходы (слева)")]
        [SerializeField] private WordSlotView[] approachSlots = new WordSlotView[6];

        [Header("Опоры (справа)")]
        [SerializeField] private WordSlotView[] supportSlots = new WordSlotView[6];

        private void Awake()
        {
            InitSlots(approachSlots);
            InitSlots(supportSlots);
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

            RefreshSlots(approachSlots, inventory.approaches);
            RefreshSlots(supportSlots,  inventory.supports);
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
