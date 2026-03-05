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

        /// <summary>Применяет дельты выбора + активные бонусы слов инвентаря. Зажимает в [0, max].</summary>
        public void ApplyChoice(EventChoice choice, WandererStatsSO stats,
                                WordInventorySO inventory = null)
        {
            lastChoice = choice;

            // Базовые дельты выбора
            int dHp  = choice.healthDelta;
            int dPow = choice.powerDelta;
            int dSan = choice.sanityDelta;

            // Суммируем активные бонусы всех слов в инвентаре (слова не тратятся)
            if (inventory != null)
            {
                foreach (var w in inventory.adjectives)
                    if (w != null) { dHp += w.activeHealthBonus; dPow += w.activePowerBonus; dSan += w.activeSanityBonus; }
                foreach (var w in inventory.nouns)
                    if (w != null) { dHp += w.activeHealthBonus; dPow += w.activePowerBonus; dSan += w.activeSanityBonus; }
            }

            health = Mathf.Clamp(health + dHp,  0, stats.maxHealth);
            power  = Mathf.Clamp(power  + dPow, 0, stats.maxPower);
            sanity = Mathf.Clamp(sanity + dSan, 0, stats.maxSanity);
        }
    }
}
