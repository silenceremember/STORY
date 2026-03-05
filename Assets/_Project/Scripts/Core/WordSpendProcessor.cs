using UnityEngine;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Применяет трату (active use) слова: снимает его из инвентаря и
    /// применяет активный бонус к GameStateSO.
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

            state.health = UnityEngine.Mathf.Clamp(
                state.health + word.activeHealthBonus, 0, stats.maxHealth);
            state.power  = UnityEngine.Mathf.Clamp(
                state.power  + word.activePowerBonus,  0, stats.maxPower);
            state.sanity = UnityEngine.Mathf.Clamp(
                state.sanity + word.activeSanityBonus, 0, stats.maxSanity);

            state.RaiseChanged();
            Debug.Log($"[WordSpendProcessor] Потрачено слово: {word.displayText}");
        }
    }
}
