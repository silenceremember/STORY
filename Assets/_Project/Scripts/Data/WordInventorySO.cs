using System;
using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Runtime-инвентарь карточек игрока.
    /// Два списка каждый (подходы / опоры).
    /// Все поля NonSerialized — сбрасываются при перезапуске через Clear().
    /// </summary>
    [CreateAssetMenu(fileName = "WordInventory", menuName = "Story/Word Inventory")]
    public class WordInventorySO : ScriptableObject
    {
        public const int MaxSlots = 3;

        [NonSerialized] public List<WordSO> approaches = new();
        [NonSerialized] public List<WordSO> supports   = new();

        [NonSerialized] private WordSO _activeApproach;
        [NonSerialized] private WordSO _activeSupport;

        public event Action OnChanged;

        // ── API ──────────────────────────────────────────────────────────────

        public void Clear()
        {
            approaches      = new List<WordSO>();
            supports        = new List<WordSO>();
            _activeApproach = null;
            _activeSupport  = null;
            OnChanged?.Invoke();
        }

        /// <summary>Пытается добавить карточку. Возвращает false если слот занят.</summary>
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
            if (_activeApproach == word) _activeApproach = null;
            if (_activeSupport  == word) _activeSupport  = null;
            OnChanged?.Invoke();
        }

        public bool IsFull(WordType type) => ListOf(type).Count >= MaxSlots;

        public List<WordSO> ListOf(WordType type) =>
            type == WordType.Approach ? approaches : supports;

        // ── Active API ────────────────────────────────────────────────────

        /// <summary>
        /// Переключает активную карточку для своего типа.
        /// Одновременно активна только 1 approach и 1 support.
        /// </summary>
        public void ToggleActive(WordSO word)
        {
            if (word == null) return;

            if (word.type == WordType.Approach)
                _activeApproach = (_activeApproach == word) ? null : word;
            else
                _activeSupport = (_activeSupport == word) ? null : word;

            OnChanged?.Invoke();
        }

        /// <summary>Возвращает текущую активную карточку для этого типа (или null).</summary>
        public WordSO GetActive(WordType type)
            => type == WordType.Approach ? _activeApproach : _activeSupport;

        public bool IsActive(WordSO word)
        {
            if (word == null) return false;
            return word.type == WordType.Approach ? word == _activeApproach : word == _activeSupport;
        }

        public void ClearActive()
        {
            _activeApproach = null;
            _activeSupport  = null;
            OnChanged?.Invoke();
        }
    }
}
