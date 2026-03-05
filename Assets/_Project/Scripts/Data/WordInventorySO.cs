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

        // ── Активные слова (подсвечены в тексте события) ─────────────────
        [NonSerialized] private HashSet<WordSO> _activeWords = new();

        public event Action OnChanged;

        // ── API ──────────────────────────────────────────────────────────────

        public void Clear()
        {
            adjectives   = new List<WordSO>();
            nouns        = new List<WordSO>();
            _activeWords = new HashSet<WordSO>();
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
            _activeWords.Remove(word);
            OnChanged?.Invoke();
        }

        public bool IsFull(WordType type) => ListOf(type).Count >= MaxSlots;

        public List<WordSO> ListOf(WordType type) =>
            type == WordType.Adjective ? adjectives : nouns;

        // ── Active API ────────────────────────────────────────────────────

        /// <summary>
        /// Переключает активное состояние слова.
        /// Активное слово: серое в инвентаре, золотое в тексте события.
        /// </summary>
        public void ToggleActive(WordSO word)
        {
            if (word == null) return;
            if (_activeWords.Contains(word)) _activeWords.Remove(word);
            else                             _activeWords.Add(word);
            OnChanged?.Invoke();
        }

        public bool IsActive(WordSO word) => word != null && _activeWords.Contains(word);

        public void ClearActive()
        {
            _activeWords.Clear();
            OnChanged?.Invoke();
        }
    }
}
