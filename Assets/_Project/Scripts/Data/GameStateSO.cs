using System;
using UnityEngine;

namespace Story.Data
{
    [CreateAssetMenu(fileName = "GameState", menuName = "Story/Game State")]
    public class GameStateSO : ScriptableObject
    {
        // ── Runtime values ──────────────────────────────
        [NonSerialized] public int day;
        [NonSerialized] public int health;
        [NonSerialized] public int supplies;
        [NonSerialized] public int sanity;
        [NonSerialized] public EventSO  currentEvent;
        [NonSerialized] public EventChoice lastChoice;   // исход предыдущего дня
        [NonSerialized] public bool isGameOver;
        [NonSerialized] public string gameOverReason;

        // ── Event for UI ─────────────────────────────────
        public event Action OnChanged;
        public event Action OnGameOver;

        public void RaiseChanged()  => OnChanged?.Invoke();
        public void RaiseGameOver() => OnGameOver?.Invoke();

        /// <summary>Сброс в начальное состояние из конфига.</summary>
        public void Initialize(WandererStatsSO stats)
        {
            day          = 1;
            health       = stats.startHealth;
            supplies     = stats.startSupplies;
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
            supplies = Mathf.Clamp(supplies + choice.suppliesDelta, 0, stats.maxSupplies);
            sanity   = Mathf.Clamp(sanity   + choice.sanityDelta,   0, stats.maxSanity);
        }
    }
}
