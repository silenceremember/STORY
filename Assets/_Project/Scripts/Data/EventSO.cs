using System;
using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Запись синергии: конкретная пара архетипов (Approach × Support)
    /// даёт собственный исход — независимо от базового успех/провал.
    ///
    /// bonusChance — дополнительный шанс за правильную комбинацию.
    /// positiveOutcomeText / negativeOutcomeText — кастомный текст исхода
    ///   (если пусто — используется дефолтный текст события).
    /// </summary>
    [Serializable]
    public class SynergyEntry
    {
        [Tooltip("Архетип карточки подхода")]
        public WordArchetype approachArchetype = WordArchetype.Physical;
        [Tooltip("Архетип карточки опоры")]
        public WordArchetype supportArchetype  = WordArchetype.Physical;

        [Tooltip("Дополнительный бонус к шансу за эту синергию")]
        [Range(-0.3f, 0.3f)]
        public float bonusChance = 0f;

        [TextArea(2, 4)]
        [Tooltip("Текст при положительном исходе. Пусто = дефолтный positiveOutcomeText события.")]
        public string positiveOutcomeText = "";
        [TextArea(2, 4)]
        [Tooltip("Текст при отрицательном исходе. Пусто = дефолтный negativeOutcomeText события.")]
        public string negativeOutcomeText = "";
    }

    /// <summary>
    /// Событие дня. Содержит:
    ///   • eventText — статичный нарратив
    ///   • totalPenalty — общий штраф (отрицательное число)
    ///   • actionVerb — глагол события (напр. «Пройти»)
    ///   • defaultApproachAdverb / defaultSupportAdverb — дефолтный составной ответ
    ///   • positiveOutcomeText / negativeOutcomeText — дефолтные последствия
    ///   • synergies — пары архетипов с собственным исходом и бонусом к шансу
    ///   • rewardWordPool — пул карточек-наград в outcome
    ///
    /// Шанс успеха = baseChance + approach.chanceModifier + support.chanceModifier + synergy.bonusChance
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
        [Tooltip("Глагол события (контекст), напр. «Пройти»")]
        public string actionVerb = "Пройти";
        [Tooltip("Глагол события в прошедшем контексте, напр. «прошёл»")]
        public string actionVerbPast = "прошёл";

        [Tooltip("Наречие подхода по умолчанию, напр. «осторожно»")]
        public string defaultApproachAdverb     = "осторожно";
        [Tooltip("Наречие подхода по умолчанию для outcome")]
        public string defaultApproachAdverbPast = "осторожно";
        [Tooltip("Распределение штрафа по HP (нормализуется вместе с pow/san)")]
        public float defaultHpWeight  = 1f;
        public float defaultPowWeight = 1f;
        public float defaultSanWeight = 1f;

        [Tooltip("Наречие опоры по умолчанию, напр. «силой»")]
        public string defaultSupportAdverb = "силой";
        [Tooltip("Уменьшение штрафа по умолчанию")]
        public int defaultPenaltyReduction = 0;

        [Header("Исход — базовый шанс")]
        [Tooltip("Базовый шанс успеха без карточек (0..1)")]
        [Range(0f, 1f)]
        public float baseChance = 0.3f;

        [Header("Исход — дефолтные тексты")]
        [TextArea(2, 4)]
        [Tooltip("Положительный исход (дефолт). Может содержать [approach]/[support] для наград.")]
        public string positiveOutcomeText = "";
        [TextArea(2, 4)]
        [Tooltip("Отрицательный исход (дефолт, без наград).")]
        public string negativeOutcomeText = "";

        [Header("Синергии")]
        [Tooltip("Пары архетипов, дающие особый исход и/или бонус к шансу")]
        public List<SynergyEntry> synergies = new();

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
        /// Ищет синергию для данной пары Approach + Support.
        /// Возвращает null, если карточка отсутствует или синергия не задана.
        /// </summary>
        public SynergyEntry FindSynergy(WordSO approach, WordSO support)
        {
            if (approach == null || support == null || synergies == null) return null;
            foreach (var s in synergies)
                if (s.approachArchetype == approach.archetype &&
                    s.supportArchetype  == support.archetype)
                    return s;
            return null;
        }

        /// <summary>
        /// Вычисляет итоговый шанс положительного исхода.
        /// = baseChance + approach.chanceModifier + support.chanceModifier + synergy.bonusChance
        /// Зажато в [0, 0.95].
        /// </summary>
        public float CalcChance(WordInventorySO inventory)
        {
            WordSO approach = inventory?.GetActive(WordType.Approach);
            WordSO support  = inventory?.GetActive(WordType.Support);
            return CalcChanceForWords(approach, support);
        }

        /// <summary>
        /// Вычисляет шанс для конкретных WordSO (для hover-превью).
        /// </summary>
        public float CalcChanceForWords(WordSO approach, WordSO support)
        {
            float chance = baseChance;

            if (approach != null) chance += approach.chanceModifier;
            if (support  != null) chance += support.chanceModifier;

            var synergy = FindSynergy(approach, support);
            if (synergy != null) chance += synergy.bonusChance;

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
        /// Строит outcome-текст.
        /// Если для пары архетипов есть SynergyEntry с непустым текстом — берётся он,
        /// иначе — дефолтный positiveOutcomeText / negativeOutcomeText события.
        /// </summary>
        public string BuildOutcome(WordSO activeApproach, WordSO activeSupport, bool isPositive)
        {
            string action = BuildActionDescription(activeApproach, activeSupport);

            var synergy = FindSynergy(activeApproach, activeSupport);

            string consequence;
            if (isPositive)
                consequence = (synergy != null && !string.IsNullOrEmpty(synergy.positiveOutcomeText))
                    ? synergy.positiveOutcomeText
                    : positiveOutcomeText;
            else
                consequence = (synergy != null && !string.IsNullOrEmpty(synergy.negativeOutcomeText))
                    ? synergy.negativeOutcomeText
                    : negativeOutcomeText;

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
            WordSO approach = inventory?.GetActive(WordType.Approach);
            WordSO support  = inventory?.GetActive(WordType.Support);
            CalcDeltasForHover(approach, support, out dHp, out dPow, out dSan);
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
