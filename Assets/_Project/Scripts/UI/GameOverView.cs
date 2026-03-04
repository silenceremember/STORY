using UnityEngine;

namespace Story.UI
{
    /// <summary>
    /// Подписывает TextButton рестарта на GameManager.StartGame().
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TextButton  restartButton;

        private void OnEnable()
        {
            if (restartButton != null)
                restartButton.OnClick += OnRestart;
        }

        private void OnDisable()
        {
            if (restartButton != null)
                restartButton.OnClick -= OnRestart;
        }

        private void OnRestart() => gameManager?.StartGame();
    }
}
