using System;
using System.Collections.Generic;
using UnityEngine;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Выбирает EventSO из базы с учётом дня, флагов и весов.
    /// Mandatory события приоритетнее обычных.
    /// </summary>
    public static class EventSelector
    {
        public static EventSO Pick(EventDatabaseSO database, int day,
                                   HashSet<string> flags, System.Random rng)
        {
            if (database == null || database.events == null || database.events.Count == 0)
            {
                Debug.LogError("[EventSelector] EventDatabase пуст или не назначен!");
                return null;
            }

            // 1. Фильтрация по дню и флагам
            var candidates = new List<EventSO>();
            foreach (var ev in database.events)
            {
                if (ev == null) continue;
                if (day < ev.dayMin || day > ev.dayMax) continue;
                if (!CheckRequiredFlags(ev, flags)) continue;
                if (!CheckExcludedFlags(ev, flags)) continue;
                candidates.Add(ev);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[EventSelector] Нет доступных событий для дня {day}! Откат на случайное.");
                return PickWeighted(database.events, rng);
            }

            // 2. Mandatory приоритет
            foreach (var ev in candidates)
            {
                if (ev.isMandatory) return ev;
            }

            // 3. Weighted random из оставшихся
            return PickWeighted(candidates, rng);
        }

        // Обратная совместимость (без дня/флагов) 
        public static EventSO Pick(EventDatabaseSO database, System.Random rng)
        {
            return Pick(database, 1, new HashSet<string>(), rng);
        }

        private static EventSO PickWeighted(List<EventSO> pool, System.Random rng)
        {
            float totalWeight = 0f;
            foreach (var ev in pool)
                if (ev != null) totalWeight += ev.weight;

            if (totalWeight <= 0f) return pool[0];

            float roll = (float)(rng.NextDouble() * totalWeight);
            float cumulative = 0f;

            foreach (var ev in pool)
            {
                if (ev == null) continue;
                cumulative += ev.weight;
                if (roll < cumulative) return ev;
            }

            return pool[pool.Count - 1];
        }

        private static bool CheckRequiredFlags(EventSO ev, HashSet<string> flags)
        {
            if (ev.requiredFlags == null || ev.requiredFlags.Length == 0) return true;
            foreach (var f in ev.requiredFlags)
            {
                if (!string.IsNullOrEmpty(f) && !flags.Contains(f)) return false;
            }
            return true;
        }

        private static bool CheckExcludedFlags(EventSO ev, HashSet<string> flags)
        {
            if (ev.excludedFlags == null || ev.excludedFlags.Length == 0) return true;
            foreach (var f in ev.excludedFlags)
            {
                if (!string.IsNullOrEmpty(f) && flags.Contains(f)) return false;
            }
            return true;
        }
    }
}
