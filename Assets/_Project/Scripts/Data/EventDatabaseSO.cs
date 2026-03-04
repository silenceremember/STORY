using System.Collections.Generic;
using UnityEngine;

namespace Story.Data
{
    [CreateAssetMenu(fileName = "EventDatabase", menuName = "Story/Event Database")]
    public class EventDatabaseSO : ScriptableObject
    {
        [Tooltip("Все доступные события. Хотя бы одно должно быть заполнено.")]
        public List<EventSO> events = new List<EventSO>();
    }
}
