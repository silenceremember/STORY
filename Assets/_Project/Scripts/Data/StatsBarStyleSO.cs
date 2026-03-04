using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Стиль полосок характеристик: цвета и пороги.
    /// </summary>
    [CreateAssetMenu(fileName = "StatsBarStyle", menuName = "Story/Config/Stats Bar Style")]
    public class StatsBarStyleSO : ScriptableObject
    {
        [Header("Цвета полоски")]
        public Color colorFull   = new Color(0.2f, 0.8f, 0.3f);
        public Color colorMedium = new Color(0.9f, 0.8f, 0.1f);
        public Color colorLow    = new Color(0.9f, 0.2f, 0.15f);

        [Header("Пороги")]
        [Range(0f, 1f)] public float mediumThreshold = 0.5f;
        [Range(0f, 1f)] public float lowThreshold    = 0.25f;
    }
}
