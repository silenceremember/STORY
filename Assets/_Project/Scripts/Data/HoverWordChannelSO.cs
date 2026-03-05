using System;
using UnityEngine;
using Story.Data;

namespace Story.Data
{
    /// <summary>
    /// SO-канал hover-событий. Слоты инвентаря пишут сюда,
    /// EventWordHighlightView читает.
    /// </summary>
    [CreateAssetMenu(fileName = "HoverWordChannel", menuName = "Story/Hover Word Channel")]
    public class HoverWordChannelSO : ScriptableObject
    {
        public event Action<WordSO> OnHoverChanged;

        public WordSO Current { get; private set; }

        public void SetHovered(WordSO word)
        {
            Current = word;
            OnHoverChanged?.Invoke(word);
        }

        private void OnDisable() => Current = null;
    }
}
