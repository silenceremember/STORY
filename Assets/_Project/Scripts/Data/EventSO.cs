using UnityEngine;

namespace Story.Data
{
    [System.Serializable]
    public class EventChoice
    {
        [Tooltip("Текст на кнопке")]
        public string label = "Выбор";

        [Tooltip("Текст, появляющийся после выбора (результат дня)")]
        [TextArea(2, 4)]
        public string outcomeText = "";

        [Header("Влияние на характеристики (отрицательные = убыль)")]
        [Range(-100, 100)] public int healthDelta   = 0;
        [Range(-100, 100)] public int powerDelta    = 0;
        [Range(-100, 100)] public int sanityDelta   = 0;

        [Header("Пул слов-наград")]
        [Tooltip("Слова, случайно выбираемые для замены токена [word] в outcomeText.\n" +
                 "Пример: «Ты нашёл [word] на дороге.»")]
        public System.Collections.Generic.List<WordSO> rewardWordPool = new();
    }

    [CreateAssetMenu(fileName = "Event_New", menuName = "Story/Event")]
    public class EventSO : ScriptableObject
    {
        [Tooltip("Нарративное описание ситуации")]
        [TextArea(3, 8)]
        public string eventText = "Описание события";

        [Tooltip("Чем выше вес, тем чаще событие выпадает")]
        [Range(0.1f, 10f)]
        public float weight = 1f;

        [Header("Варианты")]
        public EventChoice choiceA;
        public EventChoice choiceB;

        [Header("Пул слов для текста события")]
        [Tooltip("Заменяет токен [adj]/[noun] в eventText. Слова отображаются обычным цветом, подсвечиваются при hover над слотом инвентаря.")]
        public System.Collections.Generic.List<WordSO> eventWordPool = new();
    }
}
