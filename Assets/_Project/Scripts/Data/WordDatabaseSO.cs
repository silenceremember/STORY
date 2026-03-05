using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// База всех WordSO. Поиск по ключу для WordRewardProcessor.
    /// </summary>
    [CreateAssetMenu(fileName = "WordDatabase", menuName = "Story/Word Database")]
    public class WordDatabaseSO : ScriptableObject
    {
        public List<WordSO> words = new();

        public WordSO GetByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var w in words)
                if (w != null && w.key == key) return w;
            Debug.LogWarning($"[WordDatabase] Слово с ключом '{key}' не найдено.");
            return null;
        }
    }
}
