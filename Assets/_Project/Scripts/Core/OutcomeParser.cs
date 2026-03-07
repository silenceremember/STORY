using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Двухэтапный конвейер для outcome-текста:
    ///
    /// 1. PreProcess — заменяет токены [approach]/[support] случайными карточками из пула,
    ///    превращая их в [approach:key] / [support:key].
    ///
    /// 2. Parse — конвертирует [approach:key] / [support:key] в TMP rich-text строку:
    ///    золото (link) = доступно для сбора, серый = уже в инвентаре.
    /// </summary>
    public static class OutcomeParser
    {
        // Плейсхолдеры: [approach], [support]
        private static readonly Regex SlotPlaceholder =
            new Regex(@"\[(approach|support)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Типизированные токены которые выходят из PreProcess
        private static readonly Regex TypedToken =
            new Regex(@"\[(approach|support):([^\]]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public const string ColorGold = "#FFD700";
        public const string ColorGray = "#888888";
        public const string ColorRed  = "#FF4444";

        // ── Шаг 1: подстановка пула ───────────────────────────────────────

        /// <summary>
        /// Заменяет плейсхолдеры [approach], [support] карточками из пула.
        /// Каждый тип имеет независимую очередь (перемешанную).
        /// Заполняет pickedWords — реально выбранными карточками.
        /// </summary>
        public static string PreProcess(
            string       rawText,
            List<WordSO> pool,
            System.Random rng,
            List<WordSO> pickedWords)
        {
            pickedWords.Clear();

            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            if (pool == null || pool.Count == 0) return rawText;

            var approachQueue = ShuffledQueue(pool, WordType.Approach, rng);
            var supportQueue  = ShuffledQueue(pool, WordType.Support,  rng);

            return SlotPlaceholder.Replace(rawText, m =>
            {
                string slot = m.Groups[1].Value.ToLower();

                WordSO word = slot == "approach"
                    ? Dequeue(approachQueue)
                    : Dequeue(supportQueue);

                if (word == null) return string.Empty;

                pickedWords.Add(word);
                string typePart = word.type == WordType.Approach ? "approach" : "support";
                return $"[{typePart}:{word.key}]";
            });
        }

        // ── Helpers для PreProcess ────────────────────────────────────────

        private static Queue<WordSO> ShuffledQueue(
            List<WordSO>  pool,
            WordType?     filter,
            System.Random rng)
        {
            var list = new List<WordSO>();
            foreach (var w in pool)
                if (w != null && (filter == null || w.type == filter))
                    list.Add(w);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return new Queue<WordSO>(list);
        }

        private static WordSO Dequeue(Queue<WordSO> q)
            => q.Count > 0 ? q.Dequeue() : null;

        // ── Шаг 2: rich-text разметка ─────────────────────────────────────

        /// <summary>
        /// Превращает строку с [approach:key] / [support:key] в TMP rich-text (upper case).
        /// Золото = кликабельно, серый = уже в инвентаре.
        /// </summary>
        public static string Parse(
            string          processedText,
            WordDatabaseSO  db,
            WordInventorySO inventory)
        {
            if (string.IsNullOrEmpty(processedText)) return string.Empty;

            var sb = new StringBuilder();
            int lastIndex = 0;

            foreach (Match m in TypedToken.Matches(processedText))
            {
                sb.Append(processedText.Substring(lastIndex, m.Index - lastIndex)
                                       .ToUpperInvariant());

                string key  = m.Groups[2].Value;
                var    word = db?.GetByKey(key);

                if (word == null)
                {
                    sb.Append(m.Value.ToUpperInvariant());
                }
                else
                {
                    string display = word.displayText.ToUpperInvariant();
                    bool   owned   = inventory != null && IsOwned(inventory, word);

                    sb.Append(owned
                        ? $"<color={ColorGray}>{display}</color>"
                        : $"<link=\"{key}\"><color={ColorGold}>{display}</color></link>");
                }

                lastIndex = m.Index + m.Length;
            }

            sb.Append(processedText.Substring(lastIndex).ToUpperInvariant());
            return sb.ToString();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static bool IsOwned(WordInventorySO inv, WordSO word)
        {
            foreach (var w in inv.approaches) if (w == word) return true;
            foreach (var w in inv.supports)   if (w == word) return true;
            return false;
        }
    }
}
