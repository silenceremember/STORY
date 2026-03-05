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

        // ── Активное слово по типу (заменяет слот в тексте события) ──────
        [NonSerialized] private WordSO _activeAdj;
        [NonSerialized] private WordSO _activeNoun;

        public event Action OnChanged;

        // ── API ──────────────────────────────────────────────────────────────

        public void Clear()
        {
            adjectives   = new List<WordSO>();
            nouns        = new List<WordSO>();
            _activeAdj   = null;
            _activeNoun  = null;
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
            if (_activeAdj  == word) _activeAdj  = null;
            if (_activeNoun == word) _activeNoun = null;
            OnChanged?.Invoke();
        }

        public bool IsFull(WordType type) => ListOf(type).Count >= MaxSlots;

        public List<WordSO> ListOf(WordType type) =>
            type == WordType.Adjective ? adjectives : nouns;

        // ── Active API ────────────────────────────────────────────────────

        /// <summary>
        /// Переключает активное слово для своего типа.
        /// Активный adj заменяет ВСЕ [adj:key] слоты в тексте события.
        /// Активный noun заменяет ВСЕ [noun:key] слоты.
        /// Одновременно активен только 1 adj и 1 noun.
        /// </summary>
        public void ToggleActive(WordSO word)
        {
            if (word == null) return;

            if (word.type == WordType.Adjective)
                _activeAdj = (_activeAdj == word) ? null : word;
            else
                _activeNoun = (_activeNoun == word) ? null : word;

            OnChanged?.Invoke();
        }

        /// <summary>Возвращает текущее активное слово для этого типа (или null).</summary>
        public WordSO GetActive(WordType type)
            => type == WordType.Adjective ? _activeAdj : _activeNoun;

        public bool IsActive(WordSO word)
        {
            if (word == null) return false;
            return word.type == WordType.Adjective ? word == _activeAdj : word == _activeNoun;
        }

        public void ClearActive()
        {
            _activeAdj  = null;
            _activeNoun = null;
            OnChanged?.Invoke();
        }
    }
}
