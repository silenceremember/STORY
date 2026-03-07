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
    /// Flow: день → событие → кнопка с фразой (actionVerb + approach + support) → исход → карточки → след. день.
    ///
    /// ActionButton показывает:
    ///   • Event-фаза:   составная фраза («Пройти осторожно силой»)
    ///   • Outcome-фаза: «Продолжить»
    ///   • Game Over:    «Заново»
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

        [Header("Шанс + Штраф (единый блок)")]
        [SerializeField] private PenaltyPreviewView actionStats;

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
        /// <summary>True пока блок шанса/штрафа разрешён к показу (после печати action).</summary>
        private bool                          _statsVisible;

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
            HideStats();
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

        // ── Helpers: показ/скрытие единого блока ──────────────────────────

        /// <summary>Разрешает обновления из инвентаря и показывает блок с анимацией.</summary>
        private void ShowStats(float chance, EventSO ev)
        {
            _statsVisible = true;
            if (actionStats == null) return;
            ev.CalcDeltas(wordInventory, out int dHp, out int dPow, out int dSan);
            actionStats.Show(chance, dHp, dPow, dSan);
        }

        /// <summary>Скрывает блок и запрещает обновления из инвентаря/hover.</summary>
        private void HideStats()
        {
            _statsVisible = false;
            actionStats?.Hide();
        }

        // ── Обновление фразы на кнопке при изменении инвентаря ────────────

        private void OnInventoryChangedUpdatePhrase()
        {
            if (_currentLoopEvent == null) return;
            if (eventWordHighlightView != null && eventWordHighlightView.IsOutcomePhase) return;

            SetActionButtonText(_currentLoopEvent.BuildPhrase(wordInventory));

            if (!_statsVisible || actionStats == null) return;

            float chance = _currentLoopEvent.CalcChance(wordInventory);
            actionStats.UpdateChance(chance);

            _currentLoopEvent.CalcDeltas(wordInventory, out int dHp, out int dPow, out int dSan);
            actionStats.UpdatePenalty(dHp, dPow, dSan);
        }

        /// <summary>При наведении на карточку в инвентаре — превью фразы + шанса + штрафа.</summary>
        private void OnHoverUpdatePhrase(WordSO hoveredWord)
        {
            if (_currentLoopEvent == null) return;
            if (eventWordHighlightView != null && eventWordHighlightView.IsOutcomePhase) return;

            if (hoveredWord == null)
            {
                OnInventoryChangedUpdatePhrase();
                return;
            }

            var ev = _currentLoopEvent;

            // Какие карточки были бы активны, если hovered была бы применена
            WordSO previewApproach = wordInventory?.GetActive(WordType.Approach);
            WordSO previewSupport  = wordInventory?.GetActive(WordType.Support);
            if (hoveredWord.type == WordType.Approach) previewApproach = hoveredWord;
            if (hoveredWord.type == WordType.Support)  previewSupport  = hoveredWord;

            // Текст фразы на кнопке
            string approachPart = previewApproach != null && !string.IsNullOrEmpty(previewApproach.approachAdverb)
                ? previewApproach.approachAdverb
                : ev.defaultApproachAdverb;
            string supportPart = previewSupport != null && !string.IsNullOrEmpty(previewSupport.supportAdverb)
                ? previewSupport.supportAdverb
                : ev.defaultSupportAdverb;

            // Hovered карточка подсвечивается gold
            if (hoveredWord.type == WordType.Approach && !string.IsNullOrEmpty(hoveredWord.approachAdverb))
                approachPart = $"<color={OutcomeParser.ColorGold}>{hoveredWord.approachAdverb}</color>";
            else if (hoveredWord.type == WordType.Support && !string.IsNullOrEmpty(hoveredWord.supportAdverb))
                supportPart = $"<color={OutcomeParser.ColorGold}>{hoveredWord.supportAdverb}</color>";

            SetActionButtonText($"{ev.actionVerb} {approachPart} {supportPart}");

            if (!_statsVisible || actionStats == null) return;

            // Шанс с цветовым превью (учитывает archetype для синергии)
            float currentChance = ev.CalcChance(wordInventory);
            float previewChance = ev.CalcChanceForWords(previewApproach, previewSupport);
            int   pctNew        = Mathf.RoundToInt(previewChance * 100);

            if (previewChance > currentChance)
                actionStats.UpdateChance($"<color=#4CAF50>{pctNew}%</color>");
            else if (previewChance < currentChance)
                actionStats.UpdateChance($"<color=#F44336>{pctNew}%</color>");
            else
                actionStats.UpdateChance(previewChance);

            // Штраф с hover-превью
            ev.CalcDeltasForHover(previewApproach, previewSupport, out int dHp, out int dPow, out int dSan);
            actionStats.UpdatePenalty(dHp, dPow, dSan);
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

                    // 2. Показать event-текст
                    wordInventory?.ClearActive();
                    eventWordHighlightView?.SetEventPhase(ev, wordInventory);
                    await mainTypewriter.PlayAsync(ev.eventText, ct);

                    // 3. Кнопка с составной фразой — шанс/штраф скрыты до конца печати
                    string defaultPhrase  = ev.BuildPhrase(null);
                    float  defaultChance  = ev.CalcChance(null);
                    actionPanel?.SetActive(true);
                    HideStats();

                    if (actionButton != null) actionButton.Interactable = false;
                    if (actionTypewriter != null)
                        await actionTypewriter.PlayAsync(defaultPhrase, ct);
                    if (actionButton != null) actionButton.Interactable = true;

                    // Фраза напечатана — показываем блок шанса + штрафа
                    ShowStats(defaultChance, ev);

                    // 4. Ждать нажатия кнопки (фраза + шанс + штраф обновляются live)
                    _actionTcs = new UniTaskCompletionSource<bool>();
                    await _actionTcs.Task.AttachExternalCancellation(ct);

                    if (actionButton != null) actionButton.Interactable = false;
                    HideStats();

                    // 5. Рассчитать исход ПЕРЕД удалением карточек
                    bool isPositive = ev.RollOutcome(wordInventory, gameState.rng);

                    // 6. Применить approach+support + потратить активные карточки
                    var usedApproach = wordInventory?.GetActive(WordType.Approach);
                    var usedSupport  = wordInventory?.GetActive(WordType.Support);

                    bool gameOver = ChoiceProcessor.Process(ev, gameState, stats, endings, wordInventory);

                    if (usedApproach != null) wordInventory.Remove(usedApproach);
                    if (usedSupport  != null) wordInventory.Remove(usedSupport);

                    gameState.RaiseChanged();

                    // 7. Стереть event-текст и кнопку
                    var eraseMain   = mainTypewriter.EraseCurrentAsync(ct);
                    var eraseAction = actionTypewriter != null
                        ? actionTypewriter.EraseCurrentAsync(ct)
                        : UniTask.CompletedTask;
                    await UniTask.WhenAll(eraseMain, eraseAction);
                    actionPanel?.SetActive(false);

                    // 8. Показать outcome
                    _currentLoopEvent = null;

                    if (isPositive)
                        gameState.SetFlag(ev.setsFlagOnPositive);
                    else
                        gameState.SetFlag(ev.setsFlagOnNegative);

                    string outcomeRaw = ev.BuildOutcome(usedApproach, usedSupport, isPositive);

                    if (!string.IsNullOrWhiteSpace(outcomeRaw))
                    {
                        if (isPositive)
                        {
                            _pickedWords.Clear();
                            string processed = OutcomeParser.PreProcess(
                                outcomeRaw, ev.rewardWordPool, gameState.rng, _pickedWords);
                            string richText = OutcomeParser.Parse(processed, wordDatabase, wordInventory);

                            outcomeWordClickHandler?.Activate(processed);
                            eventWordHighlightView?.SetOutcomePhase(richText);
                            await mainTypewriter.PlayRichAsync(richText, ct);
                        }
                        else
                        {
                            eventWordHighlightView?.SetOutcomePhase(outcomeRaw);
                            await mainTypewriter.PlayAsync(outcomeRaw, ct);
                        }

                        // Кнопка «Продолжить»
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

                    // 9. Game Over?
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

                    // 10. Следующий день
                    gameState.day++;
                    gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.day, gameState.flags, gameState.rng);
                    dayTypewriter?.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
