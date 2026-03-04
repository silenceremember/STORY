using UnityEngine;

namespace Story.UI
{
    /// <summary>
    /// Связывает TextButton с GameManager.MakeChoice(index).
    /// </summary>
    public class ChoiceButton : MonoBehaviour
    {
        [SerializeField] private TextButton  button;
        [SerializeField] private int         choiceIndex; // 0 = A, 1 = B
        [SerializeField] private GameManager gameManager;

        private void OnEnable()
        {
            if (button != null)
                button.OnClick += OnClick;
        }

        private void OnDisable()
        {
            if (button != null)
                button.OnClick -= OnClick;
        }

        private void OnClick() => gameManager?.MakeChoice(choiceIndex);
    }
}
