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
    /// Typewriter-эффект для одного TMP_Text.
    ///
    /// Fire-and-forget API: Play(), Erase(), PlayAndErase()
    /// Awaitable API:       PlayAsync(), EraseCurrentAsync()
    ///
    /// Весь текст автоматически приводится к UPPER CASE.
    /// </summary>
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private TMP_Text targetText;

        [Header("Config")]
        [SerializeField] private TypewriterConfigSO config;

        // ── Config getters ────────────────────────────────────────────────
        private float  CharDelay    => config.charDelay;
        private float  EraseDelay   => config.eraseDelay;
        private bool   UseFadeErase => config.useFadeErase;
        private float  FadeDuration => config.fadeDuration;

        // ── State ─────────────────────────────────────────────────────────
        private CancellationTokenSource _cts;
        public  bool IsRunning { get; private set; }

        // ── Fire-and-forget API ───────────────────────────────────────────

        public void Play(string text)
        {
            CancelInternal();
            if (targetText == null) return;
            _cts = new CancellationTokenSource();
            TypeCoreAsync(Sanitize(text), _cts.Token).SuppressCancellationThrow().Forget();
        }

        public void Erase()
        {
            var current = targetText?.text ?? string.Empty;
            CancelInternal();
            if (targetText == null || string.IsNullOrEmpty(current)) return;
            _cts = new CancellationTokenSource();
            EraseCoreAsync(current, _cts.Token).SuppressCancellationThrow().Forget();
        }

        public void Clear()
        {
            CancelInternal();
            if (targetText == null) return;
            DOTween.Kill(targetText);
            targetText.text  = string.Empty;
            targetText.alpha = 1f;
        }

        public void Skip(string fullText = null)
        {
            CancelInternal();
            if (targetText == null) return;
            DOTween.Kill(targetText);
            if (fullText != null) targetText.text = Sanitize(fullText);
            targetText.alpha = 1f;
        }

        public void Cancel() => CancelInternal();

        // ── Awaitable API (используется из async game loop) ───────────────

        /// <summary>Написать текст. Бросает OperationCanceledException при отмене.</summary>
        public UniTask PlayAsync(string text, CancellationToken ct = default)
        {
            CancelInternal();
            if (targetText == null) return UniTask.CompletedTask;
            return TypeCoreAsync(Sanitize(text), ct);
        }

        /// <summary>Напечатать текст, уже стоящий на targetText (задаётся в сцене).</summary>
        public UniTask PlayCurrentAsync(CancellationToken ct = default)
        {
            var text = targetText != null ? targetText.text : string.Empty;
            CancelInternal();
            if (targetText == null || string.IsNullOrEmpty(text)) return UniTask.CompletedTask;
            return TypeCoreAsync(Sanitize(text), ct);
        }

        /// <summary>Стереть текущий текст. Бросает OperationCanceledException при отмене.</summary>
        public UniTask EraseCurrentAsync(CancellationToken ct = default)
        {
            var current = targetText?.text ?? string.Empty;
            CancelInternal();
            if (targetText == null || string.IsNullOrEmpty(current)) return UniTask.CompletedTask;
            return EraseCoreAsync(current, ct);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void OnDisable() => CancelInternal();
        private void OnDestroy() => CancelInternal();

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
            targetText.text  = string.Empty;
            targetText.alpha = 1f;
            try
            {
                for (int i = 0; i <= text.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    targetText.text = text[..i];
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
                    var tween = targetText.DOFade(0f, FadeDuration).SetEase(Ease.InQuad);
                    try
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(FadeDuration), cancellationToken: ct);
                    }
                    catch (OperationCanceledException)
                    {
                        tween.Kill();
                        throw;
                    }
                    targetText.text  = string.Empty;
                    targetText.alpha = 1f;
                }
                else
                {
                    for (int i = text.Length; i >= 0; i--)
                    {
                        ct.ThrowIfCancellationRequested();
                        targetText.text = text[..i];
                        if (i > 0)
                            await UniTask.Delay(TimeSpan.FromSeconds(EraseDelay), cancellationToken: ct);
                    }
                }
            }
            finally { IsRunning = false; }
        }
    }
}
