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
            gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.day, gameState.flags, gameState.rng);

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
                SetActionButtonText(_currentLoopEvent.BuildPhrase(wordInventory));
                return;
            }

            // Определяем verb и noun «как если бы» hovered слово было активировано
            string verbPart = null;
            string nounPart = null;

            if (wordInventory != null)
            {
                var activeVerb = wordInventory.GetActive(WordType.Verb);
                if (activeVerb != null && !string.IsNullOrEmpty(activeVerb.phraseStart))
                    verbPart = activeVerb.phraseStart;

                var activeNoun = wordInventory.GetActive(WordType.Noun);
                if (activeNoun != null && !string.IsNullOrEmpty(activeNoun.phraseEnd))
                    nounPart = activeNoun.phraseEnd;
            }

            // Hovered слово подсвечивается gold и замещает свой тип
            if (hoveredWord.type == WordType.Verb && !string.IsNullOrEmpty(hoveredWord.phraseStart))
                verbPart = $"<color={OutcomeParser.ColorGold}>{hoveredWord.phraseStart}</color>";
            else if (hoveredWord.type == WordType.Noun && !string.IsNullOrEmpty(hoveredWord.phraseEnd))
                nounPart = $"<color={OutcomeParser.ColorGold}>{hoveredWord.phraseEnd}</color>";

            // Строим фразу — дефолт НЕ смешивается с инвентарём
            bool hasVerb = verbPart != null;
            bool hasNoun = nounPart != null;

            string phrase;
            if (!hasVerb && !hasNoun)
                phrase = _currentLoopEvent.defaultPhraseStart;
            else if (hasVerb && !hasNoun)
                phrase = verbPart;
            else if (!hasVerb && hasNoun)
                phrase = $"Использовать {nounPart}";
            else
                phrase = $"{verbPart} {nounPart}";

            SetActionButtonText(phrase);
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

                    // 5. Рассчитать nature ПЕРЕД удалением слов
                    bool isPositive = ev.IsPositiveOutcome(wordInventory);

                    // 6. Применить intent+action + потратить активные слова
                    var usedVerb = wordInventory?.GetActive(WordType.Verb);
                    var usedNoun = wordInventory?.GetActive(WordType.Noun);

                    bool gameOver = ChoiceProcessor.Process(ev, gameState, stats, endings, wordInventory);

                    if (usedVerb != null) wordInventory.Remove(usedVerb);
                    if (usedNoun != null) wordInventory.Remove(usedNoun);

                    gameState.RaiseChanged();

                    // 7. Стереть event-текст и кнопку
                    var eraseMain   = mainTypewriter.EraseCurrentAsync(ct);
                    var eraseAction = actionTypewriter != null
                        ? actionTypewriter.EraseCurrentAsync(ct)
                        : UniTask.CompletedTask;
                    await UniTask.WhenAll(eraseMain, eraseAction);
                    actionPanel?.SetActive(false);

                    // 8. Показать outcome (зависит от nature)
                    _currentLoopEvent = null;

                    // Устанавливаем флаг из события
                    if (isPositive)
                        gameState.SetFlag(ev.setsFlagOnPositive);
                    else
                        gameState.SetFlag(ev.setsFlagOnNegative);

                    string outcomeRaw = ev.BuildOutcome(usedVerb, usedNoun, isPositive);

                    if (!string.IsNullOrWhiteSpace(outcomeRaw))
                    {
                        if (isPositive)
                        {
                            // Positive: кликабельные слова-награды
                            _pickedWords.Clear();
                            string processed = OutcomeParser.PreProcess(
                                outcomeRaw,
                                ev.rewardWordPool,
                                gameState.rng,
                                _pickedWords);

                            string richText = OutcomeParser.Parse(
                                processed, wordDatabase, wordInventory);

                            outcomeWordClickHandler?.Activate(processed);
                            eventWordHighlightView?.SetOutcomePhase(richText);
                            await mainTypewriter.PlayRichAsync(richText, ct);
                        }
                        else
                        {
                            // Negative: обычный текст без наград
                            eventWordHighlightView?.SetOutcomePhase(outcomeRaw);
                            await mainTypewriter.PlayAsync(outcomeRaw, ct);
                        }

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
                    gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.day, gameState.flags, gameState.rng);
                    dayTypewriter?.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
