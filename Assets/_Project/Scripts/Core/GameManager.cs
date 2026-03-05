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
    /// Flow: день → событие → кнопка с фразой (intent+action) → исход → слова → след. день.
    ///
    /// ActionButton показывает:
    ///   • Event-фаза:   составная фраза ("Молча пройти мимо")
    ///   • Outcome-фаза: "Продолжить"
    ///   • Game Over:    "Заново"
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
        [SerializeField] private HoverWordChannelSO hoverChannel;

        [Header("Typewriter — Day Label")]
        [SerializeField] private TypewriterEffect dayTypewriter;
        [SerializeField] private TypewriterEffect seedTypewriter;

        [Header("Typewriter — Main Text (Event / Outcome / Reason)")]
        [SerializeField] private TypewriterEffect mainTypewriter;

        [Header("Кнопка действия (фраза / Продолжить / Заново)")]
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
        private EventSO                       _currentLoopEvent;

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            if (actionButton != null) actionButton.OnClick += OnActionButtonClick;
            actionPanel?.SetActive(false);
        }

        private void Start() => StartGame();

        private void OnDestroy()
        {
            if (wordInventory != null) wordInventory.OnChanged -= OnInventoryChangedUpdatePhrase;
            if (hoverChannel != null)  hoverChannel.OnHoverChanged -= OnHoverUpdatePhrase;
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
            actionTypewriter?.Clear();
            eventWordHighlightView?.ClearContent();

            // Подписка на обновление фразы при изменении инвентаря / hover
            if (wordInventory != null)
            {
                wordInventory.OnChanged -= OnInventoryChangedUpdatePhrase;
                wordInventory.OnChanged += OnInventoryChangedUpdatePhrase;
            }
            if (hoverChannel != null)
            {
                hoverChannel.OnHoverChanged -= OnHoverUpdatePhrase;
                hoverChannel.OnHoverChanged += OnHoverUpdatePhrase;
            }

            RunGameLoop(_gameCts.Token).Forget();
        }

        private void OnActionButtonClick()
        {
            if (_actionTcs != null && !_actionTcs.Task.Status.IsCompleted())
                _actionTcs.TrySetResult(true);
            else
                StartGame();
        }

        // ── Обновление фразы на кнопке при изменении инвентаря ────────────

        private void OnInventoryChangedUpdatePhrase()
        {
            if (_currentLoopEvent == null) return;
            if (eventWordHighlightView != null && eventWordHighlightView.IsOutcomePhase) return;
            SetActionButtonText(_currentLoopEvent.BuildPhrase(wordInventory));
        }

        /// <summary>При наведении на слово в инвентаре — превью фразы на кнопке.</summary>
        private void OnHoverUpdatePhrase(WordSO hoveredWord)
        {
            if (_currentLoopEvent == null) return;
            if (eventWordHighlightView != null && eventWordHighlightView.IsOutcomePhase) return;

            if (hoveredWord == null)
            {
                // Снято наведение — возвращаем текущую фразу
                SetActionButtonText(_currentLoopEvent.BuildPhrase(wordInventory));
                return;
            }

            // Превью: берём текущую фразу и подставляем hovered слово
            string start = _currentLoopEvent.defaultPhraseStart;
            string end   = _currentLoopEvent.defaultPhraseEnd;

            if (wordInventory != null)
            {
                var activeAdj = wordInventory.GetActive(WordType.Adjective);
                if (activeAdj != null && !string.IsNullOrEmpty(activeAdj.phraseStart))
                    start = activeAdj.phraseStart;
                var activeNoun = wordInventory.GetActive(WordType.Noun);
                if (activeNoun != null && !string.IsNullOrEmpty(activeNoun.phraseEnd))
                    end = activeNoun.phraseEnd;
            }

            // Hovered слово заменяет свой тип (gold highlight)
            if (hoveredWord.type == WordType.Adjective && !string.IsNullOrEmpty(hoveredWord.phraseStart))
                start = $"<color={OutcomeParser.ColorGold}>{hoveredWord.phraseStart}</color>";
            else if (hoveredWord.type == WordType.Noun && !string.IsNullOrEmpty(hoveredWord.phraseEnd))
                end = $"<color={OutcomeParser.ColorGold}>{hoveredWord.phraseEnd}</color>";

            SetActionButtonText($"{start} {end}");
        }

        private void SetActionButtonText(string phrase)
        {
            if (actionTypewriter == null) return;
            var tmp = actionTypewriter.TextComponent;
            if (tmp != null)
            {
                tmp.text = phrase.ToUpperInvariant();
                tmp.maxVisibleCharacters = int.MaxValue;
            }
        }

        // ── Game Loop ─────────────────────────────────────────────────────

        private async UniTaskVoid RunGameLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var ev = gameState.currentEvent;
                    _currentLoopEvent = ev;

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

                    // 2. Показать event-текст (статичный)
                    wordInventory?.ClearActive();
                    eventWordHighlightView?.SetEventPhase(ev, wordInventory);

                    await mainTypewriter.PlayAsync(ev.eventText, ct);

                    // 3. Кнопка с составной фразой
                    string defaultPhrase = ev.BuildPhrase(null);
                    actionPanel?.SetActive(true);
                    if (actionButton != null) actionButton.Interactable = false;
                    if (actionTypewriter != null)
                        await actionTypewriter.PlayAsync(defaultPhrase, ct);
                    if (actionButton != null) actionButton.Interactable = true;

                    // 4. Ждать нажатия кнопки (фраза обновляется live через OnInventoryChanged)
                    _actionTcs = new UniTaskCompletionSource<bool>();
                    await _actionTcs.Task.AttachExternalCancellation(ct);

                    if (actionButton != null) actionButton.Interactable = false;

                    // 5. Применить intent+action + потратить активные слова
                    // Запоминаем до Process, т.к. ClearActive обнулит
                    var usedAdj  = wordInventory?.GetActive(WordType.Adjective);
                    var usedNoun = wordInventory?.GetActive(WordType.Noun);

                    bool gameOver = ChoiceProcessor.Process(ev, gameState, stats, endings, wordInventory);

                    // Расходуем использованные слова
                    if (usedAdj  != null) wordInventory.Remove(usedAdj);
                    if (usedNoun != null) wordInventory.Remove(usedNoun);

                    gameState.RaiseChanged();

                    // 6. Стереть event-текст и кнопку
                    var eraseMain   = mainTypewriter.EraseCurrentAsync(ct);
                    var eraseAction = actionTypewriter != null
                        ? actionTypewriter.EraseCurrentAsync(ct)
                        : UniTask.CompletedTask;
                    await UniTask.WhenAll(eraseMain, eraseAction);
                    actionPanel?.SetActive(false);

                    // 7. Показать outcome с кликабельными словами-наградами
                    _currentLoopEvent = null; // блокируем live-update фразы
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
