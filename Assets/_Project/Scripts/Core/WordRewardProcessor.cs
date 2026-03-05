using UnityEngine;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Выдаёт слово-награду игроку после события.
    /// Если слот занят — возвращает слово для показа в UI выбора замены.
    /// </summary>
    public static class WordRewardProcessor
    {
        /// <summary>
        /// Пытается добавить слово в инвентарь.
        /// Возвращает WordSO если нужно UI выбора замены (слот полон), иначе null.
        /// </summary>
        public static WordSO TryGiveReward(
            string           rewardKey,
            WordDatabaseSO   db,
            WordInventorySO  inventory)
        {
            if (string.IsNullOrEmpty(rewardKey)) return null;
            if (db == null || inventory == null)
            {
                Debug.LogWarning("[WordRewardProcessor] db или inventory не назначены.");
                return null;
            }

            var word = db.GetByKey(rewardKey);
            if (word == null) return null;

            if (inventory.TryAdd(word))
            {
                Debug.Log($"[WordRewardProcessor] Получено слово: {word.displayText}");
                return null; // успешно добавлено, UI не нужен
            }

            // Слот полон — нужно показать UI выбора замены
            Debug.Log($"[WordRewardProcessor] Слот {word.type} полон. Требуется выбор замены.");
            return word;
        }
    }
}
