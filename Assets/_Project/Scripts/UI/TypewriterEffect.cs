using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Typewriter-эффект. Вешается прямо на GameObject с TMP_Text.
    ///
    /// Fire-and-forget API: Play(), Erase(), Clear(), Skip()
    /// Awaitable API:       PlayAsync(), PlayCurrentAsync(), EraseCurrentAsync()
    ///
    /// Весь текст автоматически приводится к UPPER CASE.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TypewriterConfigSO config;

        // ── Config getters ────────────────────────────────────────────────
        private float  CharDelay    => config.charDelay;
        private float  EraseDelay   => config.eraseDelay;
        private bool   UseFadeErase => config.useFadeErase;
        private float  FadeDuration => config.fadeDuration;

        // ── State ─────────────────────────────────────────────────────────
        private TMP_Text                 _text;
        private string                   _originalText;  // текст из сцены, сохраняется до Awake-очистки
        private CancellationTokenSource _cts;
        public  bool IsRunning { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _originalText = _text.text;     // сохраняем до очистки
            _text.text    = string.Empty;   // скрываем до первого Play
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
            var current = _text.text;
            CancelInternal();
            if (string.IsNullOrEmpty(current)) return;
            _cts = new CancellationTokenSource();
            EraseCoreAsync(current, _cts.Token).SuppressCancellationThrow().Forget();
        }

        public void Clear()
        {
            CancelInternal();
            DOTween.Kill(_text);
            _text.text  = string.Empty;
            _text.alpha = 1f;
        }

        public void Skip(string fullText = null)
        {
            CancelInternal();
            DOTween.Kill(_text);
            if (fullText != null) _text.text = Sanitize(fullText);
            _text.alpha = 1f;
        }

        public void Cancel() => CancelInternal();

        // ── Awaitable API ─────────────────────────────────────────────────

        /// <summary>Написать текст. Бросает OperationCanceledException при отмене.</summary>
        public UniTask PlayAsync(string text, CancellationToken ct = default)
        {
            CancelInternal();
            return TypeCoreAsync(Sanitize(text), ct);
        }

        /// <summary>Напечатать текст, заданный на компоненте в сцене (сохранён до Awake-очистки).</summary>
        public UniTask PlayCurrentAsync(CancellationToken ct = default)
        {
            CancelInternal();
            if (string.IsNullOrEmpty(_originalText)) return UniTask.CompletedTask;
            return TypeCoreAsync(Sanitize(_originalText), ct);
        }

        /// <summary>Стереть текущий текст. Бросает OperationCanceledException при отмене.</summary>
        public UniTask EraseCurrentAsync(CancellationToken ct = default)
        {
            var current = _text.text;
            CancelInternal();
            if (string.IsNullOrEmpty(current)) return UniTask.CompletedTask;
            return EraseCoreAsync(current, ct);
        }

        // ── Internal ──────────────────────────────────────────────────────

        private static string Sanitize(string text)
            => string.IsNullOrEmpty(text) ? string.Empty : text.ToUpperInvariant();

        private void CancelInternal()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
            IsRunning = false;
        }

        private async UniTask TypeCoreAsync(string text, CancellationToken ct)
        {
            IsRunning = true;
            _text.text  = string.Empty;
            _text.alpha = 1f;
            try
            {
                for (int i = 0; i <= text.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    _text.text = text[..i];
                    if (i < text.Length)
                        await UniTask.Delay(TimeSpan.FromSeconds(CharDelay), cancellationToken: ct);
                }
            }
            finally { IsRunning = false; }
        }

        private async UniTask EraseCoreAsync(string text, CancellationToken ct)
        {
            IsRunning = true;
            try
            {
                if (UseFadeErase)
                {
                    var tween = _text.DOFade(0f, FadeDuration).SetEase(Ease.InQuad);
                    try
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(FadeDuration), cancellationToken: ct);
                    }
                    catch (OperationCanceledException)
                    {
                        tween.Kill();
                        throw;
                    }
                    _text.text  = string.Empty;
                    _text.alpha = 1f;
                }
                else
                {
                    for (int i = text.Length; i >= 0; i--)
                    {
                        ct.ThrowIfCancellationRequested();
                        _text.text = text[..i];
                        if (i > 0)
                            await UniTask.Delay(TimeSpan.FromSeconds(EraseDelay), cancellationToken: ct);
                    }
                }
            }
            finally { IsRunning = false; }
        }
    }
}
