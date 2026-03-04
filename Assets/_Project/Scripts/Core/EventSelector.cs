using System.Collections.Generic;
using UnityEngine;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Выбирает случайное EventSO из базы с учётом весов.
    /// </summary>
    public static class EventSelector
    {
        public static EventSO Pick(EventDatabaseSO database)
        {
            if (database == null || database.events == null || database.events.Count == 0)
            {
                Debug.LogError("[EventSelector] EventDatabase пуст или не назначен!");
                return null;
            }

            // Взвешенный случайный выбор
            float totalWeight = 0f;
            foreach (var ev in database.events)
                if (ev != null) totalWeight += ev.weight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var ev in database.events)
            {
                if (ev == null) continue;
                cumulative += ev.weight;
                if (roll < cumulative) return ev;
            }

            // Fallback — последний элемент
            return database.events[database.events.Count - 1];
        }
    }
}
