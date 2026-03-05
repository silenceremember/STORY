using System;
using UnityEngine;

namespace Story.Data
{
    [CreateAssetMenu(fileName = "GameState", menuName = "Story/Game State")]
    public class GameStateSO : ScriptableObject
    {
        // ── Runtime values ──────────────────────────────
        [NonSerialized] public int    day;
        [NonSerialized] public int    health;
        [NonSerialized] public int    power;
        [NonSerialized] public int    sanity;
        [NonSerialized] public int           seed;
        [NonSerialized] public System.Random  rng;
        [NonSerialized] public EventSO     currentEvent;
        [NonSerialized] public EventChoice lastChoice;
        [NonSerialized] public bool   isGameOver;
        [NonSerialized] public string gameOverReason;
        [NonSerialized] public WordInventorySO wordInventory;

        // ── Event for UI ─────────────────────────────────
        public event Action OnChanged;
        public event Action OnGameOver;

        public void RaiseChanged()  => OnChanged?.Invoke();
        public void RaiseGameOver() => OnGameOver?.Invoke();

        /// <summary>Сброс в начальное состояние из конфига.</summary>
        public void Initialize(WandererStatsSO stats)
        {
            seed     = new System.Random().Next(100000, 999999);
            rng      = new System.Random(seed);
            day          = 1;
            health       = stats.startHealth;
            power        = stats.startPower;
            sanity       = stats.startSanity;
            currentEvent = null;
            lastChoice   = null;
            isGameOver   = false;
            gameOverReason = string.Empty;
            RaiseChanged();
        }

        /// <summary>Применяет дельты выбора. Зажимает значения в [0, max].</summary>
        public void ApplyChoice(EventChoice choice, WandererStatsSO stats)
        {
            lastChoice = choice;
            health   = Mathf.Clamp(health   + choice.healthDelta,   0, stats.maxHealth);
            power    = Mathf.Clamp(power    + choice.powerDelta,    0, stats.maxPower);
            sanity   = Mathf.Clamp(sanity   + choice.sanityDelta,   0, stats.maxSanity);
        }
    }
}
