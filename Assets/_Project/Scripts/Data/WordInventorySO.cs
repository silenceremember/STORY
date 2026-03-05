using System;
using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Runtime-инвентарь слов игрока.
    /// Два списка до 6 слотов каждый (прилагательные / существительные).
    /// Все поля NonSerialized — сбрасываются при перезапуске через Clear().
    /// </summary>
    [CreateAssetMenu(fileName = "WordInventory", menuName = "Story/Word Inventory")]
    public class WordInventorySO : ScriptableObject
    {
        public const int MaxSlots = 6;

        [NonSerialized] public List<WordSO> adjectives = new();
        [NonSerialized] public List<WordSO> nouns      = new();

        public event Action OnChanged;

        // ── API ──────────────────────────────────────────────────────────────

        public void Clear()
        {
            adjectives = new List<WordSO>();
            nouns      = new List<WordSO>();
            OnChanged?.Invoke();
        }

        /// <summary>Пытается добавить слово. Возвращает false если слот занят.</summary>
        public bool TryAdd(WordSO word)
        {
            if (word == null) return false;
            var list = ListOf(word.type);
            if (list.Count >= MaxSlots) return false;

            list.Add(word);
            OnChanged?.Invoke();
            return true;
        }

        public void Remove(WordSO word)
        {
            if (word == null) return;
            ListOf(word.type).Remove(word);
            OnChanged?.Invoke();
        }

        public bool IsFull(WordType type) => ListOf(type).Count >= MaxSlots;

        public List<WordSO> ListOf(WordType type) =>
            type == WordType.Adjective ? adjectives : nouns;

    }
}
