using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Story.Data;

namespace Story.UI
{
    /// <summary>
    /// Кастомная текстовая кнопка.
    /// Поддерживает hover, нажатие, блокировку (interactable).
    /// Цвета и скорость перехода задаются через ButtonStyleSO.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TextButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        [Header("Style")]
        [SerializeField] private ButtonStyleSO style;

        public event Action OnClick;

        // ── State ─────────────────────────────────────────────────────────
        private TMP_Text _label;
        private bool     _isHovered;
        private bool     _isPressed;
        private bool     _interactable = true;

        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                // В неактивном состоянии — нейтральный цвет
                if (!value) ApplyColor(style.normalColor);
                else        RefreshColor();
            }
        }

        // ── Unity lifecycle ───────────────────────────────────────────────
        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
            ApplyColor(style.normalColor);
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isPressed = false;
            if (style != null && _label != null)
                ApplyColor(style.normalColor);
        }

        // ── Pointer handlers ──────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData _)
        {
            if (!_interactable) return;
            _isHovered = true;
            RefreshColor();
        }

        public void OnPointerExit(PointerEventData _)
        {
            _isHovered = false;
            _isPressed = false;
            RefreshColor();
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (!_interactable) return;
            _isPressed = true;
            RefreshColor();
        }

        public void OnPointerUp(PointerEventData _)
        {
            _isPressed = false;
            RefreshColor();
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (!_interactable) return;
            OnClick?.Invoke();
        }

        // ── Color helpers ─────────────────────────────────────────────────
        private void RefreshColor()
        {
            if (style == null) return;

            Color target = _isPressed ? style.pressedColor
                         : _isHovered ? style.hoverColor
                         : style.normalColor;
            ApplyColor(target);
        }

        private void ApplyColor(Color target)
        {
            if (_label == null) return;
            _label.color = target;
        }
    }
}
