using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Typewriter-эффект через maxVisibleCharacters — теги TMP никогда не видны.
    ///
    /// Fire-and-forget:  Play(), Erase(), Clear(), Skip()
    /// Awaitable:        PlayAsync(), PlayRichAsync(), PlayCurrentAsync(), EraseCurrentAsync()
    ///
    /// Для обычного текста: приводится к UPPER CASE.
    /// Для rich-text (PlayRichAsync): передаётся как есть (теги уже подготовлены снаружи).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TypewriterConfigSO config;

        // ── Config getters ────────────────────────────────────────────────
        private float CharDelay  => config.charDelay;
        private float EraseDelay => config.eraseDelay;

        // ── State ─────────────────────────────────────────────────────────
        private TMP_Text                _text;
        private string                  _originalText;
        private CancellationTokenSource _cts;
        public  bool IsRunning { get; private set; }

        /// <summary>Прямой доступ к TMP_Text (нужен OutcomeWordClickHandler).</summary>
        public TMP_Text TextComponent => _text;

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            _text         = GetComponent<TMP_Text>();
            _originalText = _text.text;
            Clear();
        }

        private void OnDisable() => CancelInternal();
        private void OnDestroy() => CancelInternal();

        // ── Fire-and-forget API ───────────────────────────────────────────

        public void Play(string text)
        {
            CancelInternal();
            _cts = new CancellationTokenSource();
            TypeCoreAsync(Sanitize(text), _cts.Token).SuppressCancellationThrow().Forget();
        }

        public void Erase()
        {
            CancelInternal();
            _cts = new CancellationTokenSource();
            EraseCoreAsync(_cts.Token).SuppressCancellationThrow().Forget();
        }

        public void Clear()
        {
            CancelInternal();
            _text.text                 = string.Empty;
            _text.maxVisibleCharacters = int.MaxValue;
        }

        public void Skip(string fullText = null)
        {
            CancelInternal();
            if (fullText != null) SetFull(Sanitize(fullText));
            else _text.maxVisibleCharacters = int.MaxValue;
        }

        public void Cancel() => CancelInternal();

        // ── Awaitable API ─────────────────────────────────────────────────

        /// <summary>Написать текст (UPPER CASE). Бросает OperationCanceledException при отмене.</summary>
        public UniTask PlayAsync(string text, CancellationToken ct = default)
        {
            CancelInternal();
            return TypeCoreAsync(Sanitize(text), ct);
        }

        /// <summary>
        /// Написать rich-text строку через maxVisibleCharacters.
        /// TMP-теги (color, link) сохраняются целыми.
        /// </summary>
        public UniTask PlayRichAsync(string richText, CancellationToken ct = default)
        {
            CancelInternal();
            return TypeCoreAsync(richText, ct);   // тот же алгоритм, Sanitize не нужен
        }

        /// <summary>Напечатать текст, заданный в сцене (сохранён до Awake-очистки).</summary>
        public UniTask PlayCurrentAsync(CancellationToken ct = default)
        {
            CancelInternal();
            if (string.IsNullOrEmpty(_originalText)) return UniTask.CompletedTask;
            return TypeCoreAsync(Sanitize(_originalText), ct);
        }

        /// <summary>Стереть текущий текст через maxVisibleCharacters.</summary>
        public UniTask EraseCurrentAsync(CancellationToken ct = default)
        {
            CancelInternal();
            return EraseCoreAsync(ct);
        }

        // ── Internal ──────────────────────────────────────────────────────

        private static string Sanitize(string text)
            => string.IsNullOrEmpty(text) ? string.Empty : text.ToUpperInvariant();

        private void SetFull(string text)
        {
            _text.richText             = true;
            _text.text                 = text;
            _text.maxVisibleCharacters = int.MaxValue;
            _text.ForceMeshUpdate();
        }

        private void CancelInternal()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts      = null;
            IsRunning = false;
        }

        /// <summary>
        /// Единый алгоритм набора — работает и для обычного текста, и для rich-text.
        /// Полностью устанавливает _text.text, затем приоткрывает по одному символу.
        /// </summary>
        private async UniTask TypeCoreAsync(string fullText, CancellationToken ct)
        {
            IsRunning              = true;
            _text.richText         = true;
            _text.text             = fullText;
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate();
            int total = _text.textInfo.characterCount;

            try
            {
                for (int i = 0; i <= total; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    _text.maxVisibleCharacters = i;
                    if (i < total)
                        await UniTask.Delay(TimeSpan.FromSeconds(CharDelay), cancellationToken: ct);
                }
            }
            finally
            {
                _text.maxVisibleCharacters = int.MaxValue;
                IsRunning = false;
            }
        }

        /// <summary>
        /// Стирание через убывающий maxVisibleCharacters.
        /// </summary>
        private async UniTask EraseCoreAsync(CancellationToken ct)
        {
            IsRunning = true;
            _text.ForceMeshUpdate();
            int total = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = total;

            try
            {
                for (int i = total; i >= 0; i--)
                {
                    ct.ThrowIfCancellationRequested();
                    _text.maxVisibleCharacters = i;
                    if (i > 0)
                        await UniTask.Delay(TimeSpan.FromSeconds(EraseDelay), cancellationToken: ct);
                }
                _text.text                 = string.Empty;
                _text.maxVisibleCharacters = int.MaxValue;
            }
            finally { IsRunning = false; }
        }
    }
}
