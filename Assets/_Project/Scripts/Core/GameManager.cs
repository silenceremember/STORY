using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Story.Data;
using Story.Core;
using Story.UI;

namespace Story
{
    /// <summary>
    /// Центральный оркестратор игры.
    /// Управляет async game loop: день → событие → выбор → исход → слово-награда → след. день.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("ScriptableObjects")]
        [SerializeField] private WandererStatsSO  stats;
        [SerializeField] private GameStateSO       gameState;
        [SerializeField] private EventDatabaseSO   eventDatabase;
        [SerializeField] private GameOverEndingsSO  endings;
        [SerializeField] private GameConfigSO       gameConfig;
        [SerializeField] private WordDatabaseSO    wordDatabase;
        [SerializeField] private WordInventorySO   wordInventory;

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

        [Header("Кнопка действия (Continue / Restart)")]
        [SerializeField] private TypewriterEffect actionTypewriter;
        [SerializeField] private GameObject       actionPanel;
        [SerializeField] private TextButton       actionButton;

        [Header("Outcome Word System")]
        [SerializeField] private OutcomeWordClickHandler outcomeWordClickHandler;
        [SerializeField] private EventWordHighlightView  eventWordHighlightView;

        // ── Config getters ─────────────────────────────────────────────────
        private float PauseAfterOutcome  => gameConfig.pauseAfterOutcome;
        private float PauseAfterDayLabel => gameConfig.pauseAfterDayLabel;

        // ── Private ─────────────────────────────────────────────────────
        private UniTaskCompletionSource<int>  _choiceTcs;
        private UniTaskCompletionSource<bool> _continueTcs;
        private CancellationTokenSource       _gameCts;
        private readonly List<WordSO>         _pickedWords = new();

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            if (buttonA != null) buttonA.OnClick += () => MakeChoice(0);
            if (buttonB != null) buttonB.OnClick += () => MakeChoice(1);
            if (actionButton != null) actionButton.OnClick += OnActionButtonClick;

            actionPanel?.SetActive(false);
        }

        private void Start() => StartGame();

        private void OnDestroy()
        {
            _gameCts?.Cancel();
            _gameCts?.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────

        public void StartGame()
        {
            _gameCts?.Cancel();
            _gameCts?.Dispose();
            _gameCts = new CancellationTokenSource();

            // Связываем wordInventory с gameState-ом и очищаем
            gameState.wordInventory = wordInventory;
            wordInventory?.Clear();

            gameState.Initialize(stats);
            gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.rng);

            choicePanel?.SetActive(false);
            actionPanel?.SetActive(false);
            HideRestart();
            mainTypewriter?.Clear();
            dayTypewriter?.Clear();
            seedTypewriter?.Clear();
            eventWordHighlightView?.ClearContent();

            RunGameLoop(_gameCts.Token).Forget();
        }

        public void MakeChoice(int index) => _choiceTcs?.TrySetResult(index);

        /// <summary>
        /// Единая кнопка действия:
        ///   • в outcome-фазе  → «Продолжить» (resolve _continueTcs)
        ///   • в game-over-фазе → «Начать заново» (StartGame)
        /// </summary>
        private void OnActionButtonClick()
        {
            if (_continueTcs != null && !_continueTcs.Task.Status.IsCompleted())
                _continueTcs.TrySetResult(true);
            else
                StartGame();
        }

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

                    // 2. Пре-процессинг и отображение текста события
                    {
                        var pickedEvent = new List<WordSO>();
                        string processedEvent = OutcomeParser.PreProcess(
                            ev.eventText,
                            ev.eventWordPool,
                            gameState.rng,
                            pickedEvent);
                        string richEvent = OutcomeParser.ParseEventText(
                            processedEvent, wordDatabase, null);
                        eventWordHighlightView?.SetContent(processedEvent);
                        await mainTypewriter.PlayRichAsync(richEvent, ct);
                    }

                    // 3. Показать варианты (2 кнопки)
                    choicePanel?.SetActive(true);
                    SetButtonsInteractable(false);
                    await TypeChoiceLabelsAsync(ev, ct);

                    // 4. Ждать выбора
                    _choiceTcs = new UniTaskCompletionSource<int>();
                    SetButtonsInteractable(true);
                    int choiceIndex = await _choiceTcs.Task.AttachExternalCancellation(ct);

                    // 5. Заблокировать кнопки
                    SetButtonsInteractable(false);

                    var choice = choiceIndex == 0 ? ev.choiceA : ev.choiceB;

                    // 6. Применить выбор
                    bool gameOver = ChoiceProcessor.Process(choice, gameState, stats, endings, wordInventory);
                    gameState.RaiseChanged();

                    // 7. Стереть всё одновременно
                    await EraseChoicesAsync(ct);
                    choicePanel?.SetActive(false);

                    // 8. Написать исход с кликабельными словами из пула
                    if (!string.IsNullOrWhiteSpace(choice.outcomeText))
                    {
                        // PreProcess: подбираем слова из пула для [word] токенов
                        _pickedWords.Clear();
                        string processed = OutcomeParser.PreProcess(
                            choice.outcomeText,
                            choice.rewardWordPool,
                            gameState.rng,
                            _pickedWords);

                        // Parse: строим TMP rich-text
                        string richText = OutcomeParser.Parse(
                            processed, wordDatabase, wordInventory);

                        outcomeWordClickHandler?.Activate(processed);
                        await mainTypewriter.PlayRichAsync(richText, ct);

                        // Набираем лейбл кнопки, затем ждём нажатия
                        actionPanel?.SetActive(true);
                        if (actionButton != null) actionButton.Interactable = false;
                        if (actionTypewriter != null)
                            await actionTypewriter.PlayAsync("Продолжить", ct);
                        if (actionButton != null) actionButton.Interactable = true;

                        _continueTcs = new UniTaskCompletionSource<bool>();
                        await _continueTcs.Task.AttachExternalCancellation(ct);
                        actionPanel?.SetActive(false);
                        actionTypewriter?.Clear();

                        outcomeWordClickHandler?.Deactivate();
                        await mainTypewriter.EraseCurrentAsync(ct);
                    }

                    // 9. Game Over?
                    if (gameOver)
                    {
                        await mainTypewriter.PlayAsync(gameState.gameOverReason, ct);
                        ShowRestart();
                        if (actionButton != null) actionButton.Interactable = false;
                        if (actionTypewriter != null)
                            await actionTypewriter.PlayAsync("Заново", ct);
                        if (actionButton != null) actionButton.Interactable = true;
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
            actionTypewriter?.Clear();
            actionPanel?.SetActive(false);
        }

        private void ShowRestart()
        {
            actionPanel?.SetActive(true);
        }
    }
}
