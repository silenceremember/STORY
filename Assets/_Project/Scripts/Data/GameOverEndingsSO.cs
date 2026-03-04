using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Тексты концовок при различных причинах смерти.
    /// Каждая концовка — отдельное текстовое поле, редактируемое в редакторе.
    /// </summary>
    [CreateAssetMenu(fileName = "GameOverEndings", menuName = "Story/Config/Game Over Endings")]
    public class GameOverEndingsSO : ScriptableObject
    {
        [Header("Концовки по причине смерти")]

        [Tooltip("Концовка при Здоровье = 0")]
        [TextArea(2, 5)]
        public string healthDeath;

        [Tooltip("Концовка при Припасах = 0")]
        [TextArea(2, 5)]
        public string suppliesDeath;

        [Tooltip("Концовка при Рассудке = 0")]
        [TextArea(2, 5)]
        public string sanityDeath;
    }
}