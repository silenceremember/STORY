using UnityEngine;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Применяет выбор игрока: обновляет GameStateSO и проверяет game-over.
    /// Тексты концовок берёт из GameOverEndingsSO.
    /// </summary>
    public static class ChoiceProcessor
    {
        /// <returns>true если игра завершена</returns>
        public static bool Process(
            EventChoice       choice,
            GameStateSO       state,
            WandererStatsSO   stats,
            GameOverEndingsSO endings)
        {
            state.ApplyChoice(choice, stats);

            if (state.health == 0)
            {
                state.gameOverReason = endings.healthDeath;
                return true;
            }
            if (state.power == 0)
            {
                state.gameOverReason = endings.powerDeath;
                return true;
            }
            if (state.sanity == 0)
            {
                state.gameOverReason = endings.sanityDeath;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Применяет суммарный пассивный эффект всех слов в инвентаре.
        /// Вызывается в начале каждого нового дня (до показа события).
        /// </summary>
        public static void ApplyPassiveEffects(
            GameStateSO     state,
            WordInventorySO inventory,
            WandererStatsSO stats)
        {
            if (inventory == null) return;

            state.health = Mathf.Clamp(state.health + inventory.TotalPassiveHealth, 0, stats.maxHealth);
            state.power  = Mathf.Clamp(state.power  + inventory.TotalPassivePower,  0, stats.maxPower);
            state.sanity = Mathf.Clamp(state.sanity + inventory.TotalPassiveSanity, 0, stats.maxSanity);
        }
    }
}

