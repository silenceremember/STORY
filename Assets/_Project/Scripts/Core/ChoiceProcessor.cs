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
    }
}
