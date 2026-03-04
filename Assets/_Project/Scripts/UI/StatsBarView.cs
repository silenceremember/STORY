using UnityEngine;
using UnityEngine.UI;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Отображает три полоски характеристик.
    /// Цвета и пороги задаются через StatsBarStyleSO.
    /// </summary>
    public class StatsBarView : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private GameStateSO     gameState;
        [SerializeField] private WandererStatsSO stats;
        [SerializeField] private StatsBarStyleSO style;

        [Header("UI References")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image suppliesFill;
        [SerializeField] private Image sanityFill;

        private void OnEnable()
        {
            if (gameState != null)
                gameState.OnChanged += Refresh;
        }

        private void OnDisable()
        {
            if (gameState != null)
                gameState.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            SetBar(healthFill,   gameState.health,   stats.maxHealth);
            SetBar(suppliesFill, gameState.supplies, stats.maxSupplies);
            SetBar(sanityFill,   gameState.sanity,   stats.maxSanity);
        }

        private void SetBar(Image fill, int current, int max)
        {
            if (fill == null) return;

            float t = max > 0 ? (float)current / max : 0f;
            fill.fillAmount = t;

            if (style != null)
                fill.color = t > style.mediumThreshold ? style.colorFull
                           : t > style.lowThreshold    ? style.colorMedium
                           : style.colorLow;
        }
    }
}
