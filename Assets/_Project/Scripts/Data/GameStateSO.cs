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
        [NonSerialized] public bool   isGameOver;
        [NonSerialized] public string gameOverReason;
        [NonSerialized] public WordInventorySO wordInventory;

        // ── Flags for event chaining ─────────────────────
        [NonSerialized] public System.Collections.Generic.HashSet<string> flags = new();

        public void SetFlag(string flag) { if (!string.IsNullOrEmpty(flag)) flags.Add(flag); }
        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

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
            isGameOver   = false;
            gameOverReason = string.Empty;
            flags.Clear();
            RaiseChanged();
        }

        /// <summary>
        /// Применяет intent+action: распределяет штраф из EventSO
        /// с учётом активных слов инвентаря. Зажимает в [0, max].
        /// </summary>
        public void ApplyIntentAction(EventSO ev, WandererStatsSO stats,
                                      WordInventorySO inventory = null)
        {
            ev.CalcDeltas(inventory, out int dHp, out int dPow, out int dSan);

            health = Mathf.Clamp(health + dHp,  0, stats.maxHealth);
            power  = Mathf.Clamp(power  + dPow, 0, stats.maxPower);
            sanity = Mathf.Clamp(sanity + dSan, 0, stats.maxSanity);
        }
    }
}
