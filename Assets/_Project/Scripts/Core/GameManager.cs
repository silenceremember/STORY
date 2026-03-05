using System;
using System.Threading;
using System.Collections.Generic;
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

        [Header("Restart (Game Over)")]
        [SerializeField] private TypewriterEffect restartTypewriter;
        [SerializeField] private GameObject       restartPanel;
        [SerializeField] private TextButton       restartButton;

        [Header("Word Swap Panel (появляется когда слот полон)")]
        [SerializeField] private GameObject      wordSwapPanel;
        [SerializeField] private TextButton[]    wordSwapButtons;   // 6 кнопок

        // ── Config getters ─────────────────────────────────────────────────
        private float PauseAfterOutcome  => gameConfig.pauseAfterOutcome;
        private float PauseAfterDayLabel => gameConfig.pauseAfterDayLabel;

        // ── Private ───────────────────────────────────────────────────────
        private UniTaskCompletionSource<int>     _choiceTcs;
        private UniTaskCompletionSource<WordSO>  _swapTcs;
        private CancellationTokenSource          _gameCts;

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            if (buttonA != null) buttonA.OnClick += () => MakeChoice(0);
            if (buttonB != null) buttonB.OnClick += () => MakeChoice(1);
            if (restartButton != null) restartButton.OnClick += StartGame;

            // Кнопки замены слова
            if (wordSwapButtons != null)
                for (int i = 0; i < wordSwapButtons.Length; i++)
                {
                    int idx = i;
                    if (wordSwapButtons[idx] != null)
                        wordSwapButtons[idx].OnClick += () => OnSwapSlotChosen(idx);
                }
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
            wordSwapPanel?.SetActive(false);
            HideRestart();
            mainTypewriter?.Clear();
            dayTypewriter?.Clear();
            seedTypewriter?.Clear();

            RunGameLoop(_gameCts.Token).Forget();
        }

        public void MakeChoice(int index) => _choiceTcs?.TrySetResult(index);

        // ── Game Loop ─────────────────────────────────────────────────────

        private async UniTaskVoid RunGameLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var ev = gameState.currentEvent;

                    // 0. Пассивные эффекты слов (каждый день, кроме первого)
                    if (gameState.day > 1)
                    {
                        ChoiceProcessor.ApplyPassiveEffects(gameState, wordInventory, stats);
                        gameState.RaiseChanged();
                    }

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
                    bool gameOver = ChoiceProcessor.Process(choice, gameState, stats, endings);
                    gameState.RaiseChanged();

                    // 7. Стереть всё одновременно
                    await EraseChoicesAsync(ct);
                    choicePanel?.SetActive(false);

                    // 8. Написать исход
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
                        ShowRestart();
                        if (restartTypewriter != null)
                            await restartTypewriter.PlayCurrentAsync(ct);
                        return;
                    }

                    // 10. Слово-награда
                    if (!string.IsNullOrEmpty(choice.rewardWordKey))
                        await TryHandleWordRewardAsync(choice.rewardWordKey, ct);

                    // 11. Следующий день
                    gameState.day++;
                    gameState.lastChoice   = choice;
                    gameState.currentEvent = EventSelector.Pick(eventDatabase, gameState.rng);
                    dayTypewriter?.Clear();
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Word reward ────────────────────────────────────────────────────

        /// <summary>
        /// Пытается выдать слово-награду. Если слот полон — открывает панель замены.
        /// </summary>
        private async UniTask TryHandleWordRewardAsync(string key, CancellationToken ct)
        {
            var pendingWord = WordRewardProcessor.TryGiveReward(key, wordDatabase, wordInventory);
            if (pendingWord == null) return; // добавлено без вопросов

            // Слот полон: дать игроку выбрать что выбросить или пропустить
            var discarded = await ShowWordSwapPanelAsync(pendingWord, ct);
            if (discarded != null)
            {
                wordInventory.Remove(discarded);
                wordInventory.TryAdd(pendingWord);
            }
            // Если discarded == null — игрок пропустил награду
        }

        /// <summary>
        /// Отображает панель обмена слов.
        /// Возвращает слово-замену или null если пропустить.
        /// </summary>
        private async UniTask<WordSO> ShowWordSwapPanelAsync(WordSO incoming, CancellationToken ct)
        {
            if (wordSwapPanel == null) return null;

            var list = wordInventory.ListOf(incoming.type);
            _swapTcs = new UniTaskCompletionSource<WordSO>();

            // Настраиваем кнопки
            if (wordSwapButtons != null)
            {
                for (int i = 0; i < wordSwapButtons.Length; i++)
                {
                    var btn = wordSwapButtons[i];
                    if (btn == null) continue;
                    if (i < list.Count)
                    {
                        var word = list[i];
                        // TextButton.label — предполагаем наличие публичного свойства или TMP
                        btn.gameObject.SetActive(true);
                        // Обновляем текст через TMP если есть
                        var tmp = btn.GetComponentInChildren<TMP_Text>();
                        if (tmp != null) tmp.text = word.displayText;
                    }
                    else
                    {
                        btn.gameObject.SetActive(false);
                    }
                }
            }

            wordSwapPanel.SetActive(true);
            var result = await _swapTcs.Task.AttachExternalCancellation(ct);
            wordSwapPanel.SetActive(false);
            return result;
        }

        private void OnSwapSlotChosen(int idx)
        {
            if (_swapTcs == null) return;

            // idx == -1 означает «пропустить»
            if (idx < 0)
            {
                _swapTcs.TrySetResult(null);
                return;
            }

            // Определяем тип по тому, что сейчас показывается в панели
            // Используем простой способ: смотрим adjectives + nouns в порядке
            // Панель открывается только для одного типа (incoming.type)
            // Тип нам нужен — храним его временно
            // Упрощение: отдаём первый подходящий
            if (wordInventory.adjectives.Count > idx)
                _swapTcs.TrySetResult(wordInventory.adjectives[idx]);
            else
                _swapTcs.TrySetResult(null);
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
