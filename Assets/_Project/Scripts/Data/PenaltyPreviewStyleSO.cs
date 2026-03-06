using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Стиль предпросмотра штрафа: цвета и длительность анимации.
    /// </summary>
    [CreateAssetMenu(fileName = "PenaltyPreviewStyle", menuName = "Story/Config/Penalty Preview Style")]
    public class PenaltyPreviewStyleSO : ScriptableObject
    {
        [Header("Цвета")]
        public Color negativeColor = new Color(0.96f, 0.26f, 0.21f);
        public Color neutralColor  = new Color(0.75f, 0.75f, 0.75f);
        public Color positiveColor = new Color(0.30f, 0.69f, 0.31f);

        [Header("Анимация")]
        [Range(0.1f, 2f)] public float animDuration = 0.4f;
    }
}
