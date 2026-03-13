using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultUIController : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        TryAutoAssignRestartButton();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartLevel);
            restartButton.onClick.AddListener(RestartLevel);
        }
    }

    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.GameFinished += OnGameFinished;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.GameFinished -= OnGameFinished;
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartLevel);
        }
    }

    private void OnGameFinished(bool defenderWon)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = defenderWon ? "Victory" : "Defeat";
        }
    }

    public void RestartLevel()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void TryAutoAssignRestartButton()
    {
        if (restartButton != null || panelRoot == null)
        {
            return;
        }

        Button[] buttons = panelRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            string objectName = button.gameObject.name;
            if (!string.IsNullOrEmpty(objectName) && objectName.ToLowerInvariant().Contains("restart"))
            {
                restartButton = button;
                return;
            }
        }

        if (buttons.Length > 0)
        {
            restartButton = buttons[0];
        }
    }
}
