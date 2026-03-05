using System;
using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Событие дня. Содержит:
    ///   • eventText — статичный нарратив
    ///   • totalPenalty — общий штраф (отрицательное число)
    ///   • defaultIntent / defaultAction — дефолтный составной ответ
    ///   • favorableWords — ключи слов, дающих положительный исход
    ///   • positiveOutcomeText / negativeOutcomeText — последствия
    ///   • rewardWordPool — пул слов-наград в outcome
    ///
    /// Игрок может заменить intent (verb из инвентаря) и action (noun),
    /// тем самым изменяя распределение и величину штрафа.
    /// </summary>
    [CreateAssetMenu(fileName = "Event_New", menuName = "Story/Event")]
    public class EventSO : ScriptableObject
    {
        [Header("Нарратив")]
        [Tooltip("Описание ситуации (статический текст, без слотов)")]
        [TextArea(3, 8)]
        public string eventText = "Описание события";

        [Header("Штраф")]
        [Tooltip("Общий штраф события (отрицательное число, напр. -15)")]
        public int totalPenalty = -15;

        [Header("Дефолтный ответ (всегда verb + noun)")]
        [Tooltip("Инфинитив глагола по умолчанию, напр. «Осмотреть»")]
        public string defaultPhraseStart = "Осмотреть";
        [Tooltip("2-е лицо наст. вр., напр. «осматриваешь» — для outcome")]
        public string defaultPhrasePast = "осматриваешь";
        [Tooltip("Распределение штрафа по HP (нормализуется вместе с pow/san)")]
        public float defaultHpWeight  = 1f;
        public float defaultPowWeight = 1f;
        public float defaultSanWeight = 1f;

        [Tooltip("Объект по умолчанию в вин. падеже, напр. «тропу»")]
        public string defaultPhraseEnd = "окрестности";
        [Tooltip("Уменьшение штрафа по умолчанию")]
        public int defaultPenaltyReduction = 0;

        [Header("Исход")]
        [Tooltip("Положительный исход по умолчанию (без слов)")]
        public bool defaultPositive = false;
        [Tooltip("Ключи слов, дающих положительный исход в ЭТОМ событии")]
        public List<string> favorableWords = new();

        [TextArea(2, 4)]
        [Tooltip("Положительный исход. Может содержать [verb]/[noun] для наград")]
        public string positiveOutcomeText = "";
        [TextArea(2, 4)]
        [Tooltip("Отрицательный исход (без наград)")]
        public string negativeOutcomeText = "";

        [Header("Пул слов-наград")]
        [Tooltip("Слова для [verb]/[noun] токенов в positiveOutcomeText")]
        public List<WordSO> rewardWordPool = new();

        [Header("Пул слов для отображения")]
        [Tooltip("Все слова из пула события")]
        public List<WordSO> eventWordPool = new();

        [Tooltip("Чем выше вес, тем чаще событие выпадает")]
        [Range(0.1f, 10f)]
        public float weight = 1f;

        [Header("Доступность по дням")]
        [Tooltip("С какого дня доступно (включительно)")]
        public int dayMin = 1;
        [Tooltip("По какой день доступно (включительно)")]
        public int dayMax = 10;
        [Tooltip("Блокирует все остальные события в этот день")]
        public bool isMandatory = false;

        [Header("Флаги — цепочки событий")]
        [Tooltip("Флаги, которые ДОЛЖНЫ быть установлены для доступа")]
        public string[] requiredFlags = Array.Empty<string>();
        [Tooltip("Флаги, которых НЕ ДОЛЖНО быть для доступа")]
        public string[] excludedFlags = Array.Empty<string>();
        [Tooltip("Флаг, устанавливаемый при positive outcome")]
        public string setsFlagOnPositive = "";
        [Tooltip("Флаг, устанавливаемый при negative outcome")]
        public string setsFlagOnNegative = "";

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Positive outcome если хотя бы одно активное слово есть в favorableWords.
        /// Без слов → defaultPositive (для Дня 1 = true).
        /// </summary>
        public bool IsPositiveOutcome(WordInventorySO inventory)
        {
            if (inventory == null)
                return defaultPositive;

            var verb = inventory.GetActive(WordType.Verb);
            if (verb != null && favorableWords != null && favorableWords.Contains(verb.key)) return true;

            var noun = inventory.GetActive(WordType.Noun);
            if (noun != null && favorableWords != null && favorableWords.Contains(noun.key)) return true;

            // Нет активных слов или ни одно не подошло
            bool hasAnyActive = (verb != null || noun != null);
            if (!hasAnyActive) return defaultPositive;

            return false;
        }

        /// <summary>
        /// Возвращает составную фразу для кнопки действия.
        /// Всегда verb + noun. Игрок заменяет каждую часть независимо.
        ///   0 слов → defaultPhraseStart + defaultPhraseEnd
        ///   verb  → verb.phraseStart + defaultPhraseEnd
        ///   noun  → defaultPhraseStart + noun.phraseEnd
        ///   оба   → verb.phraseStart + noun.phraseEnd
        /// </summary>
        public string BuildPhrase(WordInventorySO inventory)
        {
            string verbPart = defaultPhraseStart;
            string nounPart = defaultPhraseEnd;

            if (inventory != null)
            {
                var activeVerb = inventory.GetActive(WordType.Verb);
                if (activeVerb != null && !string.IsNullOrEmpty(activeVerb.phraseStart))
                    verbPart = activeVerb.phraseStart;

                var activeNoun = inventory.GetActive(WordType.Noun);
                if (activeNoun != null && !string.IsNullOrEmpty(activeNoun.phraseEnd))
                    nounPart = activeNoun.phraseEnd;
            }

            return $"{verbPart} {nounPart}";
        }

        /// <summary>
        /// Строит outcome-текст с описанием действия и последствием.
        /// activeVerb/activeNoun передаются явно (т.к. к моменту outcome они уже удалены из инвентаря).
        /// </summary>
        public string BuildOutcome(WordSO activeVerb, WordSO activeNoun, bool isPositive)
        {
            string action = BuildActionDescription(activeVerb, activeNoun);
            string consequence = isPositive ? positiveOutcomeText : negativeOutcomeText;
            return $"{action} {consequence}";
        }

        private string BuildActionDescription(WordSO verb, WordSO noun)
        {
            string verbPast = defaultPhrasePast;
            string nounPart = defaultPhraseEnd;

            if (verb != null && !string.IsNullOrEmpty(verb.phrasePast))
                verbPast = verb.phrasePast;
            if (noun != null && !string.IsNullOrEmpty(noun.phraseEnd))
                nounPart = noun.phraseEnd;

            return $"Ты {verbPast} {nounPart},";
        }

        /// <summary>Вычисляет итоговые дельты с учётом активных слов.</summary>
        public void CalcDeltas(WordInventorySO inventory,
                               out int dHp, out int dPow, out int dSan)
        {
            float wHp  = defaultHpWeight;
            float wPow = defaultPowWeight;
            float wSan = defaultSanWeight;

            if (inventory != null)
            {
                var activeVerb = inventory.GetActive(WordType.Verb);
                if (activeVerb != null)
                {
                    wHp  = activeVerb.hpWeight;
                    wPow = activeVerb.powWeight;
                    wSan = activeVerb.sanWeight;
                }
            }

            int reduction = defaultPenaltyReduction;
            if (inventory != null)
            {
                var activeNoun = inventory.GetActive(WordType.Noun);
                if (activeNoun != null)
                    reduction = activeNoun.penaltyReduction;
            }

            int effectivePenalty = totalPenalty + Mathf.Abs(reduction);
            if (effectivePenalty > 0) effectivePenalty = 0;

            float total = wHp + wPow + wSan;
            if (total <= 0f) total = 1f;

            dHp  = Mathf.RoundToInt(effectivePenalty * (wHp  / total));
            dPow = Mathf.RoundToInt(effectivePenalty * (wPow / total));
            dSan = Mathf.RoundToInt(effectivePenalty * (wSan / total));
        }
    }
}
