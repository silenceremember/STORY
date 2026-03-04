using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Общие параметры игры (тайминги, паузы, настройки геймплея).
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Story/Config/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Тайминги")]

        [Tooltip("Пауза после отображения исхода выбора, перед его стиранием, сек.")]
        [Range(0.5f, 10f)] public float pauseAfterOutcome;

        [Tooltip("Пауза между написанием метки дня и текста события, сек.")]
        [Range(0f, 3f)] public float pauseAfterDayLabel;
    }
}
