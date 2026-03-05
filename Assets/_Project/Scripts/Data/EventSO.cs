using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Событие дня. Содержит:
    ///   • eventText — статичный нарратив (без [adj]/[noun] токенов)
    ///   • totalPenalty — общий штраф (отрицательное число)
    ///   • defaultIntent / defaultAction — дефолтный составной ответ
    ///   • outcomeText — шаблон исхода (с [adj]/[noun] для наград)
    ///   • rewardWordPool — пул слов-наград в outcome
    ///
    /// Игрок может заменить intent (adj из инвентаря) и action (noun),
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

        [Header("Дефолтный ответ")]
        [Tooltip("Начало фразы по умолчанию, напр. «Осторожно»")]
        public string defaultPhraseStart = "Осторожно";
        [Tooltip("Распределение штрафа по HP (нормализуется вместе с pow/san)")]
        public float defaultHpWeight  = 1f;
        public float defaultPowWeight = 1f;
        public float defaultSanWeight = 1f;

        [Tooltip("Конец фразы по умолчанию, напр. «отступить»")]
        public string defaultPhraseEnd = "отступить";
        [Tooltip("Уменьшение штрафа по умолчанию")]
        public int defaultPenaltyReduction = 0;

        [Header("Исход")]
        [TextArea(2, 4)]
        [Tooltip("Текст после действия, может содержать [adj]/[noun] для наград")]
        public string outcomeText = "";

        [Header("Пул слов-наград")]
        [Tooltip("Слова для [adj]/[noun] токенов в outcomeText")]
        public List<WordSO> rewardWordPool = new();

        [Header("Пул слов для отображения")]
        [Tooltip("Все слова из пула события (для совместимости и подсветки)")]
        public List<WordSO> eventWordPool = new();

        [Tooltip("Чем выше вес, тем чаще событие выпадает")]
        [Range(0.1f, 10f)]
        public float weight = 1f;

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>Возвращает составную фразу с учётом активных слов.</summary>
        public string BuildPhrase(WordInventorySO inventory)
        {
            string start = defaultPhraseStart;
            string end   = defaultPhraseEnd;

            if (inventory != null)
            {
                var activeAdj = inventory.GetActive(WordType.Adjective);
                if (activeAdj != null && !string.IsNullOrEmpty(activeAdj.phraseStart))
                    start = activeAdj.phraseStart;

                var activeNoun = inventory.GetActive(WordType.Noun);
                if (activeNoun != null && !string.IsNullOrEmpty(activeNoun.phraseEnd))
                    end = activeNoun.phraseEnd;
            }

            return $"{start} {end}";
        }

        /// <summary>Вычисляет итоговые дельты с учётом активных слов.</summary>
        public void CalcDeltas(WordInventorySO inventory,
                               out int dHp, out int dPow, out int dSan)
        {
            // Определяем веса (из активного adj или default)
            float wHp  = defaultHpWeight;
            float wPow = defaultPowWeight;
            float wSan = defaultSanWeight;

            if (inventory != null)
            {
                var activeAdj = inventory.GetActive(WordType.Adjective);
                if (activeAdj != null)
                {
                    wHp  = activeAdj.hpWeight;
                    wPow = activeAdj.powWeight;
                    wSan = activeAdj.sanWeight;
                }
            }

            // Определяем уменьшение штрафа (из активного noun или default)
            int reduction = defaultPenaltyReduction;
            if (inventory != null)
            {
                var activeNoun = inventory.GetActive(WordType.Noun);
                if (activeNoun != null)
                    reduction = activeNoun.penaltyReduction;
            }

            // Итоговый штраф (penalty уже отрицательный, reduction уменьшает его абс. значение)
            int effectivePenalty = totalPenalty + Mathf.Abs(reduction);
            if (effectivePenalty > 0) effectivePenalty = 0; // не превращаем штраф в бонус

            // Нормализуем веса
            float total = wHp + wPow + wSan;
            if (total <= 0f) total = 1f;

            // Распределяем штраф (округляем к ближайшему)
            dHp  = Mathf.RoundToInt(effectivePenalty * (wHp  / total));
            dPow = Mathf.RoundToInt(effectivePenalty * (wPow / total));
            dSan = Mathf.RoundToInt(effectivePenalty * (wSan / total));
        }
    }
}
