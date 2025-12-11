using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages pause state for the entire game.
/// Freezes Time.timeScale and shows/hides pause menu UI.
/// </summary>
public class PauseManager : MonoBehaviour
{
    private static PauseManager instance;
    private bool isPaused = false;
    private GameObject pauseMenuUI;
    private InputAction pauseAction;

    public static PauseManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PauseManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PauseManager");
                    instance = go.AddComponent<PauseManager>();
                }
            }
            return instance;
        }
    }

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create pause input action for ESC key
        pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        pauseAction.Enable();
    }

    private void OnDestroy()
    {
        pauseAction?.Disable();
        pauseAction?.Dispose();
    }

    private void Update()
    {
        // Check for pause input (ESC key) using new Input System
        if (pauseAction.WasPressedThisFrame())
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    /// <summary>
    /// Pause the game and show pause menu
    /// </summary>
    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f; // Freeze everything
        
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Debug.Log("[PauseManager] Game Paused");
    }

    /// <summary>
    /// Resume the game and hide pause menu
    /// </summary>
    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f; // Resume time
        
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Debug.Log("[PauseManager] Game Resumed");
    }

    /// <summary>
    /// Set the pause menu UI panel reference
    /// </summary>
    public void SetPauseMenuUI(GameObject menu)
    {
        pauseMenuUI = menu;
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false); // Hide by default
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }
}
