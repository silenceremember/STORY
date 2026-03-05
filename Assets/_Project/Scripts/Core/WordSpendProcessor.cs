using UnityEngine;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Удаляет слово из инвентаря (при сборе дубликата или по другой причине).
    /// В системе Intent+Action слова не «тратятся» — этот класс оставлен для совместимости.
    /// </summary>
    public static class WordSpendProcessor
    {
        public static void Spend(
            WordSO          word,
            WordInventorySO inventory,
            GameStateSO     state,
            WandererStatsSO stats)
        {
            if (word == null || inventory == null || state == null || stats == null)
            {
                Debug.LogWarning("[WordSpendProcessor] Один из аргументов null.");
                return;
            }

            inventory.Remove(word);
            state.RaiseChanged();
            Debug.Log($"[WordSpendProcessor] Удалено слово: {word.displayText}");
        }
    }
}
