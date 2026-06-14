using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeCabinet : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private MonoBehaviour arcadeGameBehaviour;
    [SerializeField] private bool autoStartOnInteract = false;

    [Header("Arcade Screen UI")]
    [SerializeField] private GameObject arcadeCanvasRoot;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject arcadeGamePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Render Texture Screen")]
    [SerializeField] private Camera arcadeRenderCamera;
    [SerializeField] private Canvas arcadeCanvas;
    [SerializeField] private RenderTexture screenRenderTexture;
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private string screenTextureProperty = "_BaseColorMap";

    [Header("Camera / View Switching")]
    [SerializeField] private GameObject[] enableWhilePlaying;
    [SerializeField] private GameObject[] disableWhilePlaying;

    [Header("Player Scripts To Disable")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableWhilePlaying;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference startAction;
    [SerializeField] private InputActionReference exitAction;
    [SerializeField] private InputActionReference[] playerOneButtonActions = new InputActionReference[6];

    [Header("Player 1 Visual Controls")]
    [SerializeField] private ArcadeJoystickVisual playerOneJoystick;
    [SerializeField] private ArcadeButtonVisual startButtonVisual;
    [SerializeField] private ArcadeButtonVisual[] playerOneButtons = new ArcadeButtonVisual[6];

    [Header("Tickets")]
    [SerializeField] private ArcadeTicketDispenser ticketDispenser;

    private IArcadePlayable arcadeGame;
    private bool cabinetActive;
    private bool gameStarted;

    private void Awake()
    {
        arcadeGame = arcadeGameBehaviour as IArcadePlayable;

        if (arcadeGameBehaviour != null && arcadeGame == null)
        {
            Debug.LogError($"{arcadeGameBehaviour.name} must implement IArcadePlayable.");
        }

        SetupRenderTextureScreen();
        SetCabinetView(false);
        SetScreenState(false, false, false, false);
    }

    private void OnDisable()
    {
        DisableInput();

        if (cabinetActive)
            ExitCabinet();
    }

    private void Update()
    {
        if (!cabinetActive)
            return;

        UpdateControlVisuals();

        if (exitAction != null &&
            exitAction.action != null &&
            exitAction.action.WasPressedThisFrame())
        {
            ExitCabinet();
            return;
        }


        if (!gameStarted &&
            startAction != null &&
            startAction.action != null &&
            startAction.action.WasPressedThisFrame())
        {
            StartArcadeGame();
        }
    }

    public void Interact()
    {
        EnterCabinet();
    }

    public void EnterCabinet()
    {
        if (cabinetActive)
            return;

        cabinetActive = true;
        gameStarted = false;

        SetCabinetView(true);
        SetPlayerScriptsEnabled(false);
        EnableInput();

        SetScreenState(
            canvasOn: true,
            startOn: true,
            gameOn: false,
            gameOverOn: false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (autoStartOnInteract)
            StartArcadeGame();
    }

    public void ExitCabinet()
    {
        if (!cabinetActive)
            return;

        if (gameStarted && arcadeGame != null)
            arcadeGame.EndFromCabinet();

        cabinetActive = false;
        gameStarted = false;

        ResetControlVisuals();

        DisableInput();
        SetPlayerScriptsEnabled(true);
        SetCabinetView(false);

        SetScreenState(false, false, false, false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void StartArcadeGame()
    {
        if (gameStarted)
            return;

        if (arcadeGame == null)
        {
            Debug.LogWarning("No arcade game assigned to this cabinet.");
            return;
        }

        gameStarted = true;

        SetScreenState(
            canvasOn: true,
            startOn: false,
            gameOn: true,
            gameOverOn: false);

        arcadeGame.BeginFromCabinet(this);
    }

    public void ShowGameOverScreen()
    {
        SetScreenState(
            canvasOn: true,
            startOn: false,
            gameOn: true,
            gameOverOn: true);
    }
    public void DispensePlayerTickets(int amount)
    {
        if (ticketDispenser != null)
            ticketDispenser.DispenseTicketsForPlayer(amount);
    }

    public void AwardTicketsToReceiver(
        int amount,
        IArcadeTicketReceiver receiver)
    {
        if (ticketDispenser != null)
        {
            ticketDispenser.DispenseTicketsToReceiver(
                amount,
                receiver);
        }
    }

    private void SetScreenState(
        bool canvasOn,
        bool startOn,
        bool gameOn,
        bool gameOverOn)
    {
        if (arcadeCanvasRoot != null)
            arcadeCanvasRoot.SetActive(canvasOn);

        if (startPanel != null)
            startPanel.SetActive(startOn);

        if (arcadeGamePanel != null)
            arcadeGamePanel.SetActive(gameOn);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(gameOverOn);
    }

    private void SetupRenderTextureScreen()
    {
        if (arcadeRenderCamera != null)
            arcadeRenderCamera.targetTexture = screenRenderTexture;

        if (arcadeCanvas != null)
        {
            arcadeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            arcadeCanvas.worldCamera = arcadeRenderCamera;
        }

        if (screenRenderer != null && screenRenderTexture != null)
        {
            Material mat = screenRenderer.material;

            if (mat.HasProperty(screenTextureProperty))
                mat.SetTexture(screenTextureProperty, screenRenderTexture);
            else
                mat.mainTexture = screenRenderTexture;
        }
    }

    private void SetCabinetView(bool active)
    {
        foreach (GameObject obj in enableWhilePlaying)
        {
            if (obj != null)
                obj.SetActive(active);
        }

        foreach (GameObject obj in disableWhilePlaying)
        {
            if (obj != null)
                obj.SetActive(!active);
        }
    }

    private void SetPlayerScriptsEnabled(bool enabled)
    {
        foreach (MonoBehaviour script in scriptsToDisableWhilePlaying)
        {
            if (script != null)
                script.enabled = enabled;
        }
    }

    private void EnableInput()
    {
        EnableAction(moveAction);
        EnableAction(startAction);
        EnableAction(exitAction);

        foreach (InputActionReference action in playerOneButtonActions)
            EnableAction(action);
    }

    private void DisableInput()
    {
        DisableAction(moveAction);
        DisableAction(startAction);
        DisableAction(exitAction);

        foreach (InputActionReference action in playerOneButtonActions)
            DisableAction(action);
    }

    private void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
            actionReference.action.Enable();
    }

    private void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
            actionReference.action.Disable();
    }

    private void UpdateControlVisuals()
    {
        if (playerOneJoystick != null &&
            moveAction != null &&
            moveAction.action != null)
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            playerOneJoystick.SetInput(input);
        }

        SetButtonVisual(startButtonVisual, startAction);

        for (int i = 0; i < playerOneButtons.Length; i++)
        {
            InputActionReference action =
                i < playerOneButtonActions.Length
                    ? playerOneButtonActions[i]
                    : null;

            SetButtonVisual(playerOneButtons[i], action);
        }
    }

    private void SetButtonVisual(
        ArcadeButtonVisual visual,
        InputActionReference actionReference)
    {
        if (visual == null)
            return;

        bool pressed =
            actionReference != null &&
            actionReference.action != null &&
            actionReference.action.IsPressed();

        visual.SetPressed(pressed);
    }

    private void ResetControlVisuals()
    {
        if (playerOneJoystick != null)
            playerOneJoystick.ResetJoystick();

        if (startButtonVisual != null)
            startButtonVisual.SetPressed(false);

        foreach (ArcadeButtonVisual button in playerOneButtons)
        {
            if (button != null)
                button.SetPressed(false);
        }
    }
}