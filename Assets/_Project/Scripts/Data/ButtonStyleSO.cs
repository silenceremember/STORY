using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Стиль текстовой кнопки: три цвета и скорость перехода.
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonStyle", menuName = "Story/Config/Button Style")]
    public class ButtonStyleSO : ScriptableObject
    {
        [Header("Цвета текста")]
        public Color normalColor  = Color.white;
        public Color hoverColor   = new Color(0.9f, 0.85f, 0.6f);
        public Color pressedColor = new Color(0.6f, 0.55f, 0.3f);

    }
}
