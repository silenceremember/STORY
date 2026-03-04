using UnityEngine;

namespace Story.Data
{
    /// <summary>
    /// Параметры анимации typewriter.
    /// Назначается в TypewriterEffect через Inspector.
    /// Если не назначен — TypewriterEffect использует собственные сериализованные поля.
    /// </summary>
    [CreateAssetMenu(fileName = "TypewriterConfig", menuName = "Story/Config/Typewriter Config")]
    public class TypewriterConfigSO : ScriptableObject
    {
        [Header("Написание")]
        [Tooltip("Задержка между символами при написании, сек.")]
        [Range(0.01f, 0.2f)] public float charDelay;

        [Header("Стирание")]
        [Tooltip("Задержка между символами при стирании, сек.")]
        [Range(0.005f, 0.1f)] public float eraseDelay;

        [Tooltip("Если true — стирает через DOTween fade-out вместо обратного typewriter")]
        public bool useFadeErase = false;

        [Tooltip("Длительность fade-out, сек.")]
        [Range(0.1f, 2f)] public float fadeDuration;
    }
}
