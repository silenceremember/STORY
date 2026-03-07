using UnityEngine;

namespace Story.Data
{
    public enum WordType { Approach, Support }

    /// <summary>
    /// Описывает одну карточку (подход или опора).
    ///
    /// Approach → определяет «подход» (как действуешь):
    ///   approachAdverb (наречие, напр. «Осторожно») + распределение штрафа (hpWeight/powWeight/sanWeight).
    ///
    /// Support → определяет «опору» (на что опираешься):
    ///   supportAdverb (наречие, напр. «Силой») + уменьшение штрафа (penaltyReduction).
    /// </summary>
    [CreateAssetMenu(fileName = "Word_New", menuName = "Story/Word")]
    public class WordSO : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ключ")]
        public string key         = "";
        [Tooltip("Текст, отображаемый в карточке инвентаря")]
        public string displayText = "Слово";
        public WordType type      = WordType.Approach;

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

        [Header("Настроение")]
        [Tooltip("От -1 (агрессия) до +1 (помощь). Определяет positive/negative outcome.")]
        [Range(-1f, 1f)]
        public float nature = 0f;
    }
}
