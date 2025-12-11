using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the pause menu UI.
/// Handles buttons and menu interactions while paused.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel;

    private PauseManager pauseManager;

    private void Start()
    {
        pauseManager = PauseManager.Instance;

        // Register this menu with PauseManager
        pauseManager.SetPauseMenuUI(gameObject);

        // Hide by default
        gameObject.SetActive(false);

        // Register button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnResumeClicked()
    {
        pauseManager.Resume();
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[PauseMenuUI] Quitting game...");
        Time.timeScale = 1f; // Resume time before quitting
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
