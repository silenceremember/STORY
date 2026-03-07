using System;
using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Событие дня. Содержит:
    ///   • eventText — статичный нарратив
    ///   • totalPenalty — общий штраф (отрицательное число)
    ///   • actionVerb — глагол события (напр. «Пройти»), задаёт смысл действия
    ///   • defaultApproachAdverb / defaultSupportAdverb — дефолтный составной ответ
    ///   • favorableWords — ключи карточек, дающих положительный исход
    ///   • positiveOutcomeText / negativeOutcomeText — последствия
    ///   • rewardWordPool — пул карточек-наград в outcome
    ///
    /// Игрок может заменить подход (approach из инвентаря) и опору (support),
    /// тем самым изменяя распределение и величину штрафа.
    /// Фраза на кнопке: "{actionVerb} {approachAdverb} {supportAdverb}"
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

        [Header("Дефолтный ответ — глагол события + подход + опора")]
        [Tooltip("Глагол события (контекст), напр. «Пройти» — задаётся автором события")]
        public string actionVerb = "Пройти";
        [Tooltip("Глагол события в прошедшем контексте, напр. «прошёл» — для outcome")]
        public string actionVerbPast = "прошёл";

        [Tooltip("Наречие подхода по умолчанию, напр. «осторожно»")]
        public string defaultApproachAdverb     = "осторожно";
        [Tooltip("Наречие подхода по умолчанию для outcome, напр. «осторожно»")]
        public string defaultApproachAdverbPast = "осторожно";
        [Tooltip("Распределение штрафа по HP (нормализуется вместе с pow/san)")]
        public float defaultHpWeight  = 1f;
        public float defaultPowWeight = 1f;
        public float defaultSanWeight = 1f;

        [Tooltip("Наречие опоры по умолчанию, напр. «силой»")]
        public string defaultSupportAdverb = "силой";
        [Tooltip("Уменьшение штрафа по умолчанию")]
        public int defaultPenaltyReduction = 0;

        [Header("Исход — вероятности")]
        [Tooltip("Базовый шанс успеха без карточек (0..1)")]
        [Range(0f, 1f)]
        public float baseChance = 0.3f;
        [Tooltip("Бонус за каждую подходящую карточку (0..1)")]
        [Range(0f, 1f)]
        public float favorableBonus = 0.3f;
        [Tooltip("Ключи карточек, повышающих шанс в ЭТОМ событии")]
        public List<string> favorableWords = new();

        [TextArea(2, 4)]
        [Tooltip("Положительный исход. Может содержать [approach]/[support] для наград")]
        public string positiveOutcomeText = "";
        [TextArea(2, 4)]
        [Tooltip("Отрицательный исход (без наград)")]
        public string negativeOutcomeText = "";

        [Header("Пул карточек-наград")]
        [Tooltip("Карточки для [approach]/[support] токенов в positiveOutcomeText")]
        public List<WordSO> rewardWordPool = new();

        [Header("Пул карточек для отображения")]
        [Tooltip("Все карточки из пула события")]
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
        /// Вычисляет итоговый шанс положительного исхода для текущей комбинации.
        /// baseChance + matchCount × favorableBonus, зажато в [0, 0.95].
        /// </summary>
        public float CalcChance(WordInventorySO inventory)
        {
            int matches = 0;
            if (inventory != null && favorableWords != null && favorableWords.Count > 0)
            {
                var approach = inventory.GetActive(WordType.Approach);
                if (approach != null && favorableWords.Contains(approach.key)) matches++;

                var support = inventory.GetActive(WordType.Support);
                if (support != null && favorableWords.Contains(support.key)) matches++;
            }
            float chance = baseChance + matches * favorableBonus;
            return Mathf.Clamp(chance, 0f, 0.95f);
        }

        /// <summary>
        /// Вычисляет шанс для произвольных approach/support ключей (для превью при hover).
        /// </summary>
        public float CalcChanceForKeys(string approachKey, string supportKey)
        {
            int matches = 0;
            if (favorableWords != null && favorableWords.Count > 0)
            {
                if (!string.IsNullOrEmpty(approachKey) && favorableWords.Contains(approachKey)) matches++;
                if (!string.IsNullOrEmpty(supportKey)  && favorableWords.Contains(supportKey))  matches++;
            }
            float chance = baseChance + matches * favorableBonus;
            return Mathf.Clamp(chance, 0f, 0.95f);
        }

        /// <summary>
        /// Бросает кости: возвращает true (positive) с вероятностью CalcChance.
        /// </summary>
        public bool RollOutcome(WordInventorySO inventory, System.Random rng)
        {
            float chance = CalcChance(inventory);
            return (float)rng.NextDouble() < chance;
        }

        /// <summary>
        /// Возвращает составную фразу для кнопки действия.
        /// Формат: "{actionVerb} {approachAdverb} {supportAdverb}"
        ///   0 карточек → actionVerb + defaultApproachAdverb + defaultSupportAdverb
        ///   approach  → verb + approach.approachAdverb + defaultSupportAdverb
        ///   support   → verb + defaultApproachAdverb + support.supportAdverb
        ///   оба       → verb + approach.approachAdverb + support.supportAdverb
        /// </summary>
        public string BuildPhrase(WordInventorySO inventory)
        {
            string approachPart = defaultApproachAdverb;
            string supportPart  = defaultSupportAdverb;

            if (inventory != null)
            {
                var activeApproach = inventory.GetActive(WordType.Approach);
                if (activeApproach != null && !string.IsNullOrEmpty(activeApproach.approachAdverb))
                    approachPart = activeApproach.approachAdverb;

                var activeSupport = inventory.GetActive(WordType.Support);
                if (activeSupport != null && !string.IsNullOrEmpty(activeSupport.supportAdverb))
                    supportPart = activeSupport.supportAdverb;
            }

            return $"{actionVerb} {approachPart} {supportPart}";
        }

        /// <summary>
        /// Строит outcome-текст с описанием действия и последствием.
        /// activeApproach/activeSupport передаются явно (т.к. к моменту outcome они уже удалены).
        /// </summary>
        public string BuildOutcome(WordSO activeApproach, WordSO activeSupport, bool isPositive)
        {
            string action      = BuildActionDescription(activeApproach, activeSupport);
            string consequence = isPositive ? positiveOutcomeText : negativeOutcomeText;
            return $"{action} {consequence}";
        }

        private string BuildActionDescription(WordSO approach, WordSO support)
        {
            string approachPart = defaultApproachAdverbPast;
            string supportPart  = defaultSupportAdverb;

            if (approach != null && !string.IsNullOrEmpty(approach.approachAdverbPast))
                approachPart = approach.approachAdverbPast;
            if (support != null && !string.IsNullOrEmpty(support.supportAdverb))
                supportPart = support.supportAdverb;

            return $"Ты {actionVerbPast} {approachPart} {supportPart},";
        }

        /// <summary>Вычисляет итоговые дельты с учётом активных карточек.</summary>
        public void CalcDeltas(WordInventorySO inventory,
                               out int dHp, out int dPow, out int dSan)
        {
            float wHp  = defaultHpWeight;
            float wPow = defaultPowWeight;
            float wSan = defaultSanWeight;

            if (inventory != null)
            {
                var activeApproach = inventory.GetActive(WordType.Approach);
                if (activeApproach != null)
                {
                    wHp  = activeApproach.hpWeight;
                    wPow = activeApproach.powWeight;
                    wSan = activeApproach.sanWeight;
                }
            }

            int reduction = defaultPenaltyReduction;
            if (inventory != null)
            {
                var activeSupport = inventory.GetActive(WordType.Support);
                if (activeSupport != null)
                    reduction = activeSupport.penaltyReduction;
            }

            int effectivePenalty = totalPenalty + Mathf.Abs(reduction);
            if (effectivePenalty > 0) effectivePenalty = 0;

            float total = wHp + wPow + wSan;
            if (total <= 0f) total = 1f;

            dHp  = Mathf.RoundToInt(effectivePenalty * (wHp  / total));
            dPow = Mathf.RoundToInt(effectivePenalty * (wPow / total));
            dSan = Mathf.RoundToInt(effectivePenalty * (wSan / total));
        }

        /// <summary>Вычисляет дельты для конкретного подхода и опоры (для hover-превью).</summary>
        public void CalcDeltasForHover(WordSO approach, WordSO support,
                                       out int dHp, out int dPow, out int dSan)
        {
            float wHp  = approach != null ? approach.hpWeight  : defaultHpWeight;
            float wPow = approach != null ? approach.powWeight : defaultPowWeight;
            float wSan = approach != null ? approach.sanWeight : defaultSanWeight;

            int reduction = support != null ? support.penaltyReduction : defaultPenaltyReduction;

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
