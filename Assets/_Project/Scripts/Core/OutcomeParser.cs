using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Story.Data;

namespace Story.Core
{
    /// <summary>
    /// Двухэтапный конвейер для outcome-текста:
    ///
    /// 1. PreProcess — заменяет токены [word] случайными словами из пула,
    ///    превращая их в [adj:key] / [noun:key].
    ///
    /// 2. Parse — конвертирует [adj:key] / [noun:key] в TMP rich-text строку:
    ///    золото (link) = доступно, серый = уже в инвентаре.
    ///
    /// Пример outcomeText: "Ты нашёл [word] воина и взял его [word]."
    /// При пуле [brave, sword] → "...нашёл [adj:brave] воина и взял его [noun:sword]."
    /// </summary>
    public static class OutcomeParser
    {
        // Плейсхолдеры: [adj], [noun]
        private static readonly Regex SlotPlaceholder =
            new Regex(@"\[(adj|noun)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Типизированные токены которые выходят из PreProcess
        private static readonly Regex TypedToken =
            new Regex(@"\[(adj|noun):([a-zA-Z_]+)\]", RegexOptions.Compiled);

        public const string ColorGold = "#FFD700";
        public const string ColorGray = "#888888";
        public const string ColorRed  = "#FF4444";

        // ── Шаг 1: подстановка пула ───────────────────────────────────────

        /// <summary>
        /// Заменяет плейсхолдеры [adj], [noun] словами из пула.
        ///
        ///   [adj]  → только прилагательные из пула
        ///   [noun] → только существительные из пула
        ///
        /// Каждый тип имеет независимую очередь (перемешанную).
        /// Заполняет pickedWords — реально выбранными словами.
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

            // Две независимые перемешанные очереди
            var adjQueue  = ShuffledQueue(pool, WordType.Adjective, rng);
            var nounQueue = ShuffledQueue(pool, WordType.Noun,      rng);

            return SlotPlaceholder.Replace(rawText, m =>
            {
                string slot = m.Groups[1].Value.ToLower();

                WordSO word = slot == "adj"
                    ? Dequeue(adjQueue)
                    : Dequeue(nounQueue);

                if (word == null) return string.Empty;

                pickedWords.Add(word);
                string typePart = word.type == WordType.Adjective ? "adj" : "noun";
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

            // Fisher-Yates
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
        /// Превращает строку с [adj:key] / [noun:key] в TMP rich-text (upper case).
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

        // ── Event text (normal colour + hover/active highlight) ────────────

        /// <summary>
        /// Возвращает множество ключей слов, встроенных в processedText.
        /// </summary>
        public static HashSet<string> GetKeywordsInText(string processedText)
        {
            var keys = new HashSet<string>();
            if (!string.IsNullOrEmpty(processedText))
                foreach (Match m in TypedToken.Matches(processedText))
                    keys.Add(m.Groups[2].Value);
            return keys;
        }

        /// <summary>
        /// Рендерит текст события:
        ///   • Активные слова (inventory.IsActive) → золотые;
        ///   • Hovered слово → золотое;
        ///   • Остальные → обычный цвет.
        /// </summary>
        public static string ParseEventText(
            string          processedText,
            WordDatabaseSO  db,
            WordSO          hoveredWord,
            WordInventorySO inventory = null)
        {
            if (string.IsNullOrEmpty(processedText)) return string.Empty;

            var sb        = new StringBuilder();
            int lastIndex = 0;

            foreach (Match m in TypedToken.Matches(processedText))
            {
                sb.Append(processedText.Substring(lastIndex, m.Index - lastIndex)
                                       .ToUpperInvariant());

                string key     = m.Groups[2].Value;
                var    word    = db?.GetByKey(key);
                string display = word != null
                    ? word.displayText.ToUpperInvariant()
                    : key.ToUpperInvariant();

                bool isActive  = inventory != null && word != null && inventory.IsActive(word);
                bool highlight = hoveredWord != null && word == hoveredWord;

                sb.Append(isActive || highlight
                    ? $"<color={ColorGold}>{display}</color>"
                    : display);

                lastIndex = m.Index + m.Length;
            }

            sb.Append(processedText.Substring(lastIndex).ToUpperInvariant());
            return sb.ToString();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static bool IsOwned(WordInventorySO inv, WordSO word)
        {
            foreach (var w in inv.adjectives) if (w == word) return true;
            foreach (var w in inv.nouns)      if (w == word) return true;
            return false;
        }
    }
}
