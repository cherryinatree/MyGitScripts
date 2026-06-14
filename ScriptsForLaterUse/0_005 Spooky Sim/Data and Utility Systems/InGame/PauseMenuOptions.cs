using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // optional, only if you use LoadScene below

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuOptions : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private GameObject pauseMenuRoot;  // Canvas (or panel) to show when paused

    [Header("Options")]
    [Tooltip("If true, the pause UI is hidden on Start.")]
    [SerializeField] private bool startHidden = true;

    private bool isPaused;
    private float prevTimeScale = 1f;
    private CursorLockMode prevLockState = CursorLockMode.None;
    private bool prevCursorVisible = true;

    public InGameSaveManager saveManager; // Reference to the in-game save manager

#if ENABLE_INPUT_SYSTEM
    private InputAction pauseAction;
#endif

    private void Awake()
    {
        Time.timeScale = 1f;
        if (pauseMenuRoot == null)
        {
            Debug.LogError($"{nameof(PauseMenuOptions)}: Pause Menu Root is not assigned.");
        }
        if (pauseMenuRoot && startHidden) pauseMenuRoot.SetActive(false);

        if(saveManager == null)
        {
            saveManager = FindObjectOfType<InGameSaveManager>();
            if (saveManager == null)
            {
                Debug.LogError($"{nameof(PauseMenuOptions)}: InGameSaveManager not found in the scene.");
            }
        }
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        // New Input System – listen for Escape
        pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
        pauseAction.performed += OnPausePerformed;
        pauseAction.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
            pauseAction.Dispose();
            pauseAction = null;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnPausePerformed(InputAction.CallbackContext ctx) => TogglePause();
#endif

    private void Update()
    {
#if !ENABLE_INPUT_SYSTEM
        // Legacy Input fallback
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
#endif
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        // Store previous states
        prevTimeScale = Time.timeScale;
        prevLockState = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        // Apply pause
        Time.timeScale = 0f;

        if (pauseMenuRoot) pauseMenuRoot.SetActive(true);

        // Give the player mouse control
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // (Optional) auto-select a default UI element
        // if (EventSystem.current) EventSystem.current.SetSelectedGameObject(pauseMenuRoot);
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        // Restore states
        Time.timeScale = prevTimeScale;
        if (pauseMenuRoot) pauseMenuRoot.SetActive(false);

        Cursor.lockState = prevLockState;
        Cursor.visible = prevCursorVisible;
    }

    // ---- Hook these from UI Buttons if desired ----
    public void UI_Resume() => Resume();

    public void Continue()
    {
        Resume();
    }

    public void Settings()
    {

       // Implement settings menu logic here
        Debug.Log("Settings button clicked.");
    }

    public void LoadGame()
    {
    
        saveManager.LoadGame();
    }

    public void SaveGame()
    {

        saveManager.SaveGame();
    }


    public void SaveAndQuitToMainMenu()
    {
        // Ensure time scale is normal
        Time.timeScale = 1f;

        saveManager.SaveGame();
        // Load main menu scene (replace "MainMenu" with your scene name)
        SceneManager.LoadScene("MainMenu");
    }
}
