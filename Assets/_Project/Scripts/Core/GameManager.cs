using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Story.Data;
using Story.Core;
using Story.UI;

namespace Story
{
    /// <summary>
    /// Центральный оркестратор игры.
    /// Управляет async game loop: день → событие → выбор → исход → следующий день.
    /// Все зависимости назначаются через Inspector.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private WandererStatsSO  stats;
        [SerializeField] private GameStateSO       gameState;
        [SerializeField] private EventDatabaseSO   eventDatabase;
        [SerializeField] private GameOverEndingsSO  endings;
        [SerializeField] private GameConfigSO       gameConfig;

        [Header("Typewriter — Day Label")]
        [SerializeField] private TypewriterEffect dayTypewriter;

        [Header("Typewriter — Main Text (Event / Outcome / Reason)")]
        [SerializeField] private TypewriterEffect mainTypewriter;

        [Header("Choices Panel")]
        [SerializeField] private GameObject          choicePanel;
        [SerializeField] private TypewriterEffect    choiceTypewriterA;
        [SerializeField] private TypewriterEffect    choiceTypewriterB;
        [SerializeField] private TextButton          buttonA;
        [SerializeField] private TextButton          buttonB;

        [Header("Restart (Game Over)")]
        [SerializeField] private TypewriterEffect restartTypewriter;
        [SerializeField] private TextButton       restartButton;

        // ── Config getters ─────────────────────────────────────────────────
        private float PauseAfterOutcome  => gameConfig.pauseAfterOutcome;
        private float PauseAfterDayLabel => gameConfig.pauseAfterDayLabel;

        // ── Private ───────────────────────────────────────────────────────
        private UniTaskCompletionSource<int> _choiceTcs;
        private CancellationTokenSource      _gameCts;

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Start() => StartGame();

        private void OnDestroy()
        {
            _gameCts?.Cancel();
            _gameCts?.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Запуск / перезапуск игры.</summary>
        public void StartGame()
        {
            _gameCts?.Cancel();
            _gameCts?.Dispose();
            _gameCts = new CancellationTokenSource();

            gameState.Initialize(stats);
            gameState.currentEvent = EventSelector.Pick(eventDatabase);

            choicePanel?.SetActive(false);
            if (restartButton != null)
            {
                restartButton.Interactable = false;
                restartTypewriter?.Clear();
            }
            mainTypewriter?.Clear();
            dayTypewriter?.Clear();

            RunGameLoop(_gameCts.Token).Forget();
        }

        /// <summary>Вызывается кнопками выбора (Choice A = 0, Choice B = 1).</summary>
        public void MakeChoice(int index) => _choiceTcs?.TrySetResult(index);

        // ── Game Loop ─────────────────────────────────────────────────────

        private async UniTaskVoid RunGameLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var ev = gameState.currentEvent;

                    // 1. Написать метку дня
                    if (dayTypewriter != null)
                    {
                        await dayTypewriter.PlayAsync($"День {gameState.day}", ct);
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(PauseAfterDayLabel),
                            cancellationToken: ct);
                    }

                    // 2. Написать текст события
                    await mainTypewriter.PlayAsync(ev.eventText, ct);

                    // 3. Написать варианты выбора (A, затем B)
                    choicePanel?.SetActive(true);
                    SetButtonsInteractable(false);
                    await TypeChoiceLabelsAsync(ev, ct);
                    SetButtonsInteractable(true);

                    // 4. Ждать выбора игрока
                    _choiceTcs = new UniTaskCompletionSource<int>();
                    int choiceIndex = await _choiceTcs.Task.AttachExternalCancellation(ct);

                    // 5. Скрыть варианты
                    choicePanel?.SetActive(false);

                    var choice = choiceIndex == 0 ? ev.choiceA : ev.choiceB;

                    // 6. Применить выбор к состоянию
                    bool gameOver = ChoiceProcessor.Process(choice, gameState, stats, endings);
                    gameState.RaiseChanged(); // StatsBarView обновит полоски

                    // 7. Стереть текст события
                    await mainTypewriter.EraseCurrentAsync(ct);

                    // 8. Написать исход (если есть)
                    if (!string.IsNullOrWhiteSpace(choice.outcomeText))
                    {
                        await mainTypewriter.PlayAsync(choice.outcomeText, ct);
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(PauseAfterOutcome),
                            cancellationToken: ct);
                        await mainTypewriter.EraseCurrentAsync(ct);
                    }

                    // 9. Game Over?
                    if (gameOver)
                    {
                        await mainTypewriter.PlayAsync(gameState.gameOverReason, ct);
                        if (restartTypewriter != null)
                            await restartTypewriter.PlayCurrentAsync(ct);
                        if (restartButton != null)
                            restartButton.Interactable = true;
                        return;
                    }

                    // 10. Следующий день
                    gameState.day++;
                    gameState.lastChoice   = choice;
                    gameState.currentEvent = EventSelector.Pick(eventDatabase);
                    dayTypewriter?.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private async UniTask TypeChoiceLabelsAsync(EventSO ev, CancellationToken ct)
        {
            if (choiceTypewriterA != null) await choiceTypewriterA.PlayAsync(ev.choiceA.label, ct);
            if (choiceTypewriterB != null) await choiceTypewriterB.PlayAsync(ev.choiceB.label, ct);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (buttonA != null) buttonA.Interactable = value;
            if (buttonB != null) buttonB.Interactable = value;
        }
    }
}
