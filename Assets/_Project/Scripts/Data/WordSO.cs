using UnityEngine;

namespace Story.Data
{
    public enum WordType { Verb, Noun }

    /// <summary>
    /// Описывает одно слово (глагол или существительное).
    ///
    /// Verb → определяет «намерение» (intent):
    ///   phraseStart (инфинитив глагола) + распределение штрафа (hpWeight/powWeight/sanWeight).
    ///
    /// Noun → определяет «действие» (action):
    ///   phraseEnd (объект в вин. падеже с контекстом) + уменьшение штрафа (penaltyReduction).
    /// </summary>
    [CreateAssetMenu(fileName = "Word_New", menuName = "Story/Word")]
    public class WordSO : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ключ")]
        public string key         = "";
        [Tooltip("Текст, отображаемый в инвентарном слоте")]
        public string displayText = "Слово";
        public WordType type      = WordType.Verb;

        [Header("Intent — только для verb")]
        [Tooltip("Инфинитив глагола, напр. «Метнуть»")]
        public string phraseStart = "";
        [Tooltip("2-е лицо наст. вр., напр. «швыряешь» — для outcome-текста")]
        public string phrasePast = "";
        [Tooltip("Доля штрафа → HP (нормализуется с pow/san)")]
        public float hpWeight  = 1f;
        [Tooltip("Доля штрафа → POW")]
        public float powWeight = 1f;
        [Tooltip("Доля штрафа → SAN")]
        public float sanWeight = 1f;

        [Header("Action — только для noun")]
        [Tooltip("Объект с контекстом в вин. падеже, напр. «острый кинжал»")]
        public string phraseEnd = "";
        [Tooltip("На сколько единиц уменьшается общий штраф при использовании")]
        public int penaltyReduction = 0;

        [Header("Настроение")]
        [Tooltip("От -1 (агрессия) до +1 (помощь). Определяет positive/negative outcome.")]
        [Range(-1f, 1f)]
        public float nature = 0f;
    }
}
