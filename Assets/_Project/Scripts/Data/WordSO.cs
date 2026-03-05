using UnityEngine;

namespace Story.Data
{
    public enum WordType { Adjective, Noun }

    /// <summary>
    /// Описывает одно слово (прилагательное или существительное).
    /// Пассивный эффект работает каждый день, активный — при трате слова.
    /// </summary>
    [CreateAssetMenu(fileName = "Word_New", menuName = "Story/Word")]
    public class WordSO : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ключ, используется в EventChoice.rewardWordKey")]
        public string key         = "";
        [Tooltip("Текст, отображаемый в инвентарном слоте")]
        public string displayText = "Слово";
        public WordType type      = WordType.Adjective;

        [Header("Активный эффект (при трате)")]
        public int activeHealthBonus   = 0;
        public int activePowerBonus    = 0;
        public int activeSanityBonus   = 0;
        [TextArea(1, 3)]
        public string activeDescription = "";

    }
}
