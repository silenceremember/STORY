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
        [SerializeField] private TypewriterEffect seedTypewriter;

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
        [SerializeField] private GameObject       restartPanel;
        [SerializeField] private TextButton       restartButton;

        // ── Config getters ─────────────────────────────────────────────────
        private float PauseAfterOutcome  => gameConfig.pauseAfterOutcome;
        private float PauseAfterDayLabel => gameConfig.pauseAfterDayLabel;

        // ── Private ───────────────────────────────────────────────────────
        private UniTaskCompletionSource<int> _choiceTcs;
        private CancellationTokenSource      _gameCts;

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            if (buttonA != null) buttonA.OnClick += () => MakeChoice(0);
            if (buttonB != null) buttonB.OnClick += () => MakeChoice(1);
            if (restartButton != null) restartButton.OnClick += StartGame;
        }

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
            gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.rng);

            choicePanel?.SetActive(false);
            HideRestart();
            mainTypewriter?.Clear();
            dayTypewriter?.Clear();
            seedTypewriter?.Clear();

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

                    // 1. Написать метку дня + seed (в первый день одновременно)
                    if (dayTypewriter != null)
                    {
                        var dayTask  = dayTypewriter.PlayAsync($"День {gameState.day}", ct);
                        var seedTask = gameState.day == 1 && seedTypewriter != null
                            ? seedTypewriter.PlayAsync($"Seed {gameState.seed}", ct)
                            : UniTask.CompletedTask;
                        await UniTask.WhenAll(dayTask, seedTask);
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(PauseAfterDayLabel),
                            cancellationToken: ct);
                    }

                    // 2. Написать текст события
                    await mainTypewriter.PlayAsync(ev.eventText, ct);

                    // 3. Печатать варианты одновременно
                    choicePanel?.SetActive(true);
                    SetButtonsInteractable(false);
                    await TypeChoiceLabelsAsync(ev, ct);

                    // 4. Ждать выбора (сначала TCS, затем разблокировать кнопки)
                    _choiceTcs = new UniTaskCompletionSource<int>();
                    SetButtonsInteractable(true);
                    int choiceIndex = await _choiceTcs.Task.AttachExternalCancellation(ct);

                    // 5. Заблокировать кнопки сразу
                    SetButtonsInteractable(false);

                    var choice = choiceIndex == 0 ? ev.choiceA : ev.choiceB;

                    // 6. Применить выбор к состоянию
                    bool gameOver = ChoiceProcessor.Process(choice, gameState, stats, endings);
                    gameState.RaiseChanged();

                    // 7. Стереть всё одновременно: текст события + оба варианта
                    await EraseChoicesAsync(ct);
                    choicePanel?.SetActive(false);

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
                        ShowRestart(); // SetActive(true) до запуска typewriter
                        if (restartTypewriter != null)
                            await restartTypewriter.PlayCurrentAsync(ct);
                        return;
                    }

                    // 10. Следующий день
                    gameState.day++;
                    gameState.lastChoice   = choice;
                    gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.rng);
                    dayTypewriter?.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private UniTask EraseChoicesAsync(CancellationToken ct)
        {
            var eraseMain = mainTypewriter.EraseCurrentAsync(ct);
            var eraseA    = choiceTypewriterA != null
                ? choiceTypewriterA.EraseCurrentAsync(ct)
                : UniTask.CompletedTask;
            var eraseB    = choiceTypewriterB != null
                ? choiceTypewriterB.EraseCurrentAsync(ct)
                : UniTask.CompletedTask;
            return UniTask.WhenAll(eraseMain, eraseA, eraseB);
        }

        private UniTask TypeChoiceLabelsAsync(EventSO ev, CancellationToken ct)
        {
            var taskA = choiceTypewriterA != null
                ? choiceTypewriterA.PlayAsync(ev.choiceA.label, ct)
                : UniTask.CompletedTask;
            var taskB = choiceTypewriterB != null
                ? choiceTypewriterB.PlayAsync(ev.choiceB.label, ct)
                : UniTask.CompletedTask;
            return UniTask.WhenAll(taskA, taskB);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (buttonA != null) buttonA.Interactable = value;
            if (buttonB != null) buttonB.Interactable = value;
        }

        private void HideRestart()
        {
            restartTypewriter?.Clear();
            restartPanel?.SetActive(false);
        }

        private void ShowRestart()
        {
            restartPanel?.SetActive(true);
        }
    }
}
