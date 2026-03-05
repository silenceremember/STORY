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
    /// Flow: день → событие → составной ответ (intent+action) → исход → слова → след. день.
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

        [Header("Составная фраза действия")]
        [SerializeField] private TypewriterEffect phraseTypewriter;

        [Header("Кнопка действия (Действовать / Продолжить / Заново)")]
        [SerializeField] private TypewriterEffect actionTypewriter;
        [SerializeField] private GameObject       actionPanel;
        [SerializeField] private TextButton       actionButton;

        [Header("Outcome Word System")]
        [SerializeField] private OutcomeWordClickHandler outcomeWordClickHandler;
        [SerializeField] private EventWordHighlightView  eventWordHighlightView;

        // ── Config getters ─────────────────────────────────────────────────
        private float PauseAfterDayLabel => gameConfig.pauseAfterDayLabel;

        // ── Private ─────────────────────────────────────────────────────
        private UniTaskCompletionSource<bool> _actionTcs;
        private CancellationTokenSource       _gameCts;
        private readonly List<WordSO>         _pickedWords = new();

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
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

            gameState.wordInventory = wordInventory;
            wordInventory?.Clear();

            gameState.Initialize(stats);
            gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.rng);

            actionPanel?.SetActive(false);
            mainTypewriter?.Clear();
            dayTypewriter?.Clear();
            seedTypewriter?.Clear();
            phraseTypewriter?.Clear();
            eventWordHighlightView?.ClearContent();

            RunGameLoop(_gameCts.Token).Forget();
        }

        /// <summary>
        /// Единая кнопка действия:
        ///   • в event-фазе → «Действовать» (resolve _actionTcs)
        ///   • в outcome-фазе → «Продолжить» (resolve _actionTcs)
        ///   • в game-over → «Заново» (StartGame)
        /// </summary>
        private void OnActionButtonClick()
        {
            if (_actionTcs != null && !_actionTcs.Task.Status.IsCompleted())
                _actionTcs.TrySetResult(true);
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

                    // 1. Метка дня + seed
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

                    // 2. Показать event-текст (статичный, без токенов)
                    wordInventory?.ClearActive();
                    eventWordHighlightView?.SetEventPhase(ev, wordInventory);

                    await mainTypewriter.PlayAsync(ev.eventText, ct);

                    // 3. Показать составную фразу + кнопку "Действовать"
                    //    Фраза обновляется при активации слов (через OnChanged)
                    string defaultPhrase = ev.BuildPhrase(null);
                    if (phraseTypewriter != null)
                        await phraseTypewriter.PlayAsync(defaultPhrase, ct);

                    actionPanel?.SetActive(true);
                    if (actionButton != null) actionButton.Interactable = false;
                    if (actionTypewriter != null)
                        await actionTypewriter.PlayAsync("Действовать", ct);
                    if (actionButton != null) actionButton.Interactable = true;

                    // 4. Ждать нажатия "Действовать"
                    _actionTcs = new UniTaskCompletionSource<bool>();
                    await _actionTcs.Task.AttachExternalCancellation(ct);

                    if (actionButton != null) actionButton.Interactable = false;

                    // 5. Применить intent+action
                    bool gameOver = ChoiceProcessor.Process(ev, gameState, stats, endings, wordInventory);
                    gameState.RaiseChanged();

                    // 6. Стереть event-текст и фразу
                    var eraseMain   = mainTypewriter.EraseCurrentAsync(ct);
                    var erasePhrase = phraseTypewriter != null
                        ? phraseTypewriter.EraseCurrentAsync(ct)
                        : UniTask.CompletedTask;
                    var eraseAction = actionTypewriter != null
                        ? actionTypewriter.EraseCurrentAsync(ct)
                        : UniTask.CompletedTask;
                    await UniTask.WhenAll(eraseMain, erasePhrase, eraseAction);
                    actionPanel?.SetActive(false);

                    // 7. Показать outcome с кликабельными словами-наградами
                    if (!string.IsNullOrWhiteSpace(ev.outcomeText))
                    {
                        _pickedWords.Clear();
                        string processed = OutcomeParser.PreProcess(
                            ev.outcomeText,
                            ev.rewardWordPool,
                            gameState.rng,
                            _pickedWords);

                        string richText = OutcomeParser.Parse(
                            processed, wordDatabase, wordInventory);

                        outcomeWordClickHandler?.Activate(processed);
                        eventWordHighlightView?.SetOutcomePhase(richText);
                        await mainTypewriter.PlayRichAsync(richText, ct);

                        // Кнопка "Продолжить"
                        actionPanel?.SetActive(true);
                        if (actionButton != null) actionButton.Interactable = false;
                        if (actionTypewriter != null)
                            await actionTypewriter.PlayAsync("Продолжить", ct);
                        if (actionButton != null) actionButton.Interactable = true;

                        _actionTcs = new UniTaskCompletionSource<bool>();
                        await _actionTcs.Task.AttachExternalCancellation(ct);
                        actionPanel?.SetActive(false);
                        actionTypewriter?.Clear();

                        outcomeWordClickHandler?.Deactivate();
                        eventWordHighlightView?.ClearContent();
                        await mainTypewriter.EraseCurrentAsync(ct);
                    }

                    // 8. Game Over?
                    if (gameOver)
                    {
                        await mainTypewriter.PlayAsync(gameState.gameOverReason, ct);
                        actionPanel?.SetActive(true);
                        if (actionButton != null) actionButton.Interactable = false;
                        if (actionTypewriter != null)
                            await actionTypewriter.PlayAsync("Заново", ct);
                        if (actionButton != null) actionButton.Interactable = true;
                        return;
                    }

                    // 9. Следующий день
                    gameState.day++;
                    gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.rng);
                    dayTypewriter?.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
