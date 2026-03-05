using System;
using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Runtime-инвентарь слов игрока.
    /// Два списка до 6 слотов каждый (глаголы / существительные).
    /// Все поля NonSerialized — сбрасываются при перезапуске через Clear().
    /// </summary>
    [CreateAssetMenu(fileName = "WordInventory", menuName = "Story/Word Inventory")]
    public class WordInventorySO : ScriptableObject
    {
        public const int MaxSlots = 6;

        [NonSerialized] public List<WordSO> verbs = new();
        [NonSerialized] public List<WordSO> nouns = new();

        [NonSerialized] private WordSO _activeVerb;
        [NonSerialized] private WordSO _activeNoun;

        public event Action OnChanged;

        // ── API ──────────────────────────────────────────────────────────────

        public void Clear()
        {
            verbs       = new List<WordSO>();
            nouns       = new List<WordSO>();
            _activeVerb = null;
            _activeNoun = null;
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
            if (_activeVerb == word) _activeVerb = null;
            if (_activeNoun == word) _activeNoun = null;
            OnChanged?.Invoke();
        }

        public bool IsFull(WordType type) => ListOf(type).Count >= MaxSlots;

        public List<WordSO> ListOf(WordType type) =>
            type == WordType.Verb ? verbs : nouns;

        // ── Active API ────────────────────────────────────────────────────

        /// <summary>
        /// Переключает активное слово для своего типа.
        /// Одновременно активен только 1 verb и 1 noun.
        /// </summary>
        public void ToggleActive(WordSO word)
        {
            if (word == null) return;

            if (word.type == WordType.Verb)
                _activeVerb = (_activeVerb == word) ? null : word;
            else
                _activeNoun = (_activeNoun == word) ? null : word;

            OnChanged?.Invoke();
        }

        /// <summary>Возвращает текущее активное слово для этого типа (или null).</summary>
        public WordSO GetActive(WordType type)
            => type == WordType.Verb ? _activeVerb : _activeNoun;

        public bool IsActive(WordSO word)
        {
            if (word == null) return false;
            return word.type == WordType.Verb ? word == _activeVerb : word == _activeNoun;
        }

        public void ClearActive()
        {
            _activeVerb = null;
            _activeNoun = null;
            OnChanged?.Invoke();
        }
    }
}
