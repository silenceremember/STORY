using UnityEngine;

namespace Story.Data
{
    public enum WordType { Approach, Support }

    /// <summary>
    /// Архетип карточки — определяет синергию при комбинировании Approach + Support.
    /// Physical  = сила, тело, прямое действие
    /// Mental    = ум, хитрость, знание
    /// Social    = речь, доверие, убеждение
    /// Mystical  = магия, ритуал, неизведанное
    /// </summary>
    public enum WordArchetype { Physical, Mental, Social, Mystical }

    /// <summary>
    /// Описывает одну карточку (подход или опора).
    ///
    /// Approach → определяет «подход» (как действуешь):
    ///   approachAdverb + распределение штрафа (hpWeight/powWeight/sanWeight) + chanceModifier.
    ///
    /// Support → определяет «опору» (на что опираешься):
    ///   supportAdverb + уменьшение штрафа (penaltyReduction) + chanceModifier.
    ///
    /// archetype — архетип карточки для расчёта синергий (Approach.archetype × Support.archetype).
    /// chanceModifier — базовый вклад карточки в шанс успеха (применяется всегда, для обоих типов).
    /// </summary>
    [CreateAssetMenu(fileName = "Word_New", menuName = "Story/Word")]
    public class WordSO : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ключ")]
        public string key         = "";
        [Tooltip("Текст, отображаемый в карточке инвентаря")]
        public string displayText = "Слово";
        public WordType      type      = WordType.Approach;
        public WordArchetype archetype = WordArchetype.Physical;

        [Header("Шанс")]
        [Tooltip("Базовый вклад карточки в шанс успеха (±). Работает для Approach и Support.")]
        [Range(-0.3f, 0.3f)]
        public float chanceModifier = 0f;

        [Header("Approach — только для карточек подхода")]
        [Tooltip("Наречие подхода, напр. «Осторожно» — для кнопки действия")]
        public string approachAdverb     = "";
        [Tooltip("Наречие подхода для outcome-текста, напр. «осторожно»")]
        public string approachAdverbPast = "";
        [Tooltip("Доля штрафа → HP (нормализуется с pow/san)")]
        public float hpWeight  = 1f;
        [Tooltip("Доля штрафа → POW")]
        public float powWeight = 1f;
        [Tooltip("Доля штрафа → SAN")]
        public float sanWeight = 1f;

        [Header("Support — только для карточек опоры")]
        [Tooltip("Наречие опоры, напр. «Силой» — для кнопки действия и outcome-текста")]
        public string supportAdverb = "";
        [Tooltip("На сколько единиц уменьшается общий штраф при использовании")]
        public int penaltyReduction = 0;
    }
}
