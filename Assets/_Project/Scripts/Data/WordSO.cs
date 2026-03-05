using UnityEngine;

namespace Story.Data
{
    public enum WordType { Adjective, Noun }

    /// <summary>
    /// Описывает одно слово (прилагательное или существительное).
    ///
    /// Adjective → определяет «намерение» (intent):
    ///   phraseStart + распределение штрафа по статам (hpWeight/powWeight/sanWeight).
    ///
    /// Noun → определяет «действие» (action):
    ///   phraseEnd + величину уменьшения штрафа (penaltyReduction).
    /// </summary>
    [CreateAssetMenu(fileName = "Word_New", menuName = "Story/Word")]
    public class WordSO : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ключ")]
        public string key         = "";
        [Tooltip("Текст, отображаемый в инвентарном слоте")]
        public string displayText = "Слово";
        public WordType type      = WordType.Adjective;

        [Header("Intent — только для adj")]
        [Tooltip("Начало составной фразы ответа, напр. «Храбро»")]
        public string phraseStart = "";
        [Tooltip("Доля штрафа → HP (нормализуется с pow/san)")]
        public float hpWeight  = 1f;
        [Tooltip("Доля штрафа → POW")]
        public float powWeight = 1f;
        [Tooltip("Доля штрафа → SAN")]
        public float sanWeight = 1f;

        [Header("Action — только для noun")]
        [Tooltip("Конец составной фразы ответа, напр. «ударить мечом»")]
        public string phraseEnd = "";
        [Tooltip("На сколько единиц уменьшается общий штраф при использовании")]
        public int penaltyReduction = 0;
    }
}
