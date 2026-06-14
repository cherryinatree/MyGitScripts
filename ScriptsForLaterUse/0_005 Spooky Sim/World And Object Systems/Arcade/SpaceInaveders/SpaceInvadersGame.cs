using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpaceInvadersGame : MonoBehaviour
{
    [Header("Game Area")]
    [SerializeField] private RectTransform gamePanel;

    [Header("Sprites")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite bulletSprite;
    [SerializeField] private Sprite[] invaderSprites;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference fireAction;

    [Header("Disable While Playing")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;

    [Header("Player")]
    [SerializeField] private float playerSpeed = 700f;
    [SerializeField] private float bulletSpeed = 1200f;

    [Header("Invaders")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 11;
    [SerializeField] private float invaderSpeed = 80f;
    [SerializeField] private float invaderDropDistance = 30f;
    [SerializeField] private float invaderStepInterval = 0.5f;

    private RectTransform player;
    private RectTransform playerBullet;

    private readonly List<RectTransform> invaders =
        new List<RectTransform>();

    private bool movingRight = true;
    private bool gameActive;

    private float stepTimer;

    private int score;
    private int lives = 3;

    #region Public

    public void BeginGame()
    {
        if (gameActive)
            return;

        gameActive = true;

        gamePanel.gameObject.SetActive(true);

        foreach (var script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        score = 0;
        lives = 3;

        ClearObjects();

        CreatePlayer();
        CreateInvaders();

        UpdateUI();

        moveAction.action.Enable();
        fireAction.action.Enable();

        fireAction.action.performed += OnFirePressed;

        stepTimer = invaderStepInterval;
    }

    public void EndGame()
    {
        gameActive = false;

        fireAction.action.performed -= OnFirePressed;

        moveAction.action.Disable();
        fireAction.action.Disable();

        foreach (var script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ClearObjects();

        gamePanel.gameObject.SetActive(false);
    }

    #endregion

    private void Update()
    {
        if (!gameActive)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndGame();
            return;
        }

        UpdatePlayer();
        UpdateBullet();
        UpdateInvaders();
    }

    private void CreatePlayer()
    {
        GameObject go = new GameObject("Player");

        go.transform.SetParent(gamePanel, false);

        Image image = go.AddComponent<Image>();
        image.sprite = playerSprite;

        player = go.GetComponent<RectTransform>();

        player.sizeDelta = new Vector2(64, 64);

        player.anchoredPosition = new Vector2(
            0,
            -gamePanel.rect.height * 0.40f);
    }

    private void CreateInvaders()
    {
        invaders.Clear();

        float spacingX = 60f;
        float spacingY = 50f;

        float formationWidth =
            (columns - 1) * spacingX;

        float startX =
            -formationWidth * 0.5f;

        float startY =
            gamePanel.rect.height * 0.30f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject alien =
                    new GameObject($"Alien_{row}_{col}");

                alien.transform.SetParent(
                    gamePanel,
                    false);

                Image image =
                    alien.AddComponent<Image>();

                if (invaderSprites.Length > 0)
                {
                    image.sprite =
                        invaderSprites[
                            Mathf.Min(
                                row,
                                invaderSprites.Length - 1)];
                }

                RectTransform rect =
                    alien.GetComponent<RectTransform>();

                rect.sizeDelta =
                    new Vector2(48, 48);

                rect.anchoredPosition =
                    new Vector2(
                        startX + col * spacingX,
                        startY - row * spacingY);

                invaders.Add(rect);
            }
        }
    }

    private void UpdatePlayer()
    {
        float move =
            moveAction.action.ReadValue<float>();

        Vector2 pos =
            player.anchoredPosition;

        pos.x +=
            move *
            playerSpeed *
            Time.deltaTime;

        float limit =
            gamePanel.rect.width * 0.5f - 40f;

        pos.x =
            Mathf.Clamp(
                pos.x,
                -limit,
                limit);

        player.anchoredPosition = pos;
    }

    private void OnFirePressed(
        InputAction.CallbackContext ctx)
    {
        if (!gameActive)
            return;

        if (playerBullet != null)
            return;

        GameObject bullet =
            new GameObject("Bullet");

        bullet.transform.SetParent(
            gamePanel,
            false);

        Image image =
            bullet.AddComponent<Image>();

        image.sprite = bulletSprite;

        playerBullet =
            bullet.GetComponent<RectTransform>();

        playerBullet.sizeDelta =
            new Vector2(10, 24);

        playerBullet.anchoredPosition =
            player.anchoredPosition +
            Vector2.up * 35f;
    }

    private void UpdateBullet()
    {
        if (playerBullet == null)
            return;

        playerBullet.anchoredPosition +=
            Vector2.up *
            bulletSpeed *
            Time.deltaTime;

        for (int i = invaders.Count - 1; i >= 0; i--)
        {
            if (invaders[i] == null)
                continue;

            float distance =
                Vector2.Distance(
                    playerBullet.anchoredPosition,
                    invaders[i].anchoredPosition);

            if (distance < 30f)
            {
                Destroy(invaders[i].gameObject);

                invaders.RemoveAt(i);

                Destroy(playerBullet.gameObject);

                playerBullet = null;

                score += 10;

                UpdateUI();

                AdjustInvaderSpeed();

                if (invaders.Count == 0)
                {
                    EndGame();
                }

                return;
            }
        }

        if (playerBullet.anchoredPosition.y >
            gamePanel.rect.height * 0.5f)
        {
            Destroy(playerBullet.gameObject);
            playerBullet = null;
        }
    }

    private void UpdateInvaders()
    {
        stepTimer -= Time.deltaTime;

        if (stepTimer > 0)
            return;

        stepTimer = invaderStepInterval;

        bool hitWall = false;

        float edge =
            gamePanel.rect.width * 0.5f - 50f;

        foreach (RectTransform invader in invaders)
        {
            if (invader == null)
                continue;

            Vector2 pos =
                invader.anchoredPosition;

            pos.x +=
                movingRight
                    ? invaderSpeed
                    : -invaderSpeed;

            invader.anchoredPosition = pos;

            if (pos.x > edge ||
                pos.x < -edge)
            {
                hitWall = true;
            }
        }

        if (hitWall)
        {
            movingRight = !movingRight;

            foreach (RectTransform invader in invaders)
            {
                if (invader == null)
                    continue;

                invader.anchoredPosition +=
                    Vector2.down *
                    invaderDropDistance;

                if (invader.anchoredPosition.y <
                    player.anchoredPosition.y + 50f)
                {
                    EndGame();
                    return;
                }
            }
        }
    }

    private void AdjustInvaderSpeed()
    {
        float remaining =
            invaders.Count /
            (float)(rows * columns);

        invaderStepInterval =
            Mathf.Lerp(
                0.08f,
                0.5f,
                remaining);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text =
                $"Score: {score}";

        if (livesText != null)
            livesText.text =
                $"Lives: {lives}";
    }

    private void ClearObjects()
    {
        List<GameObject> destroyList =
            new List<GameObject>();

        foreach (Transform child in gamePanel)
        {
            destroyList.Add(child.gameObject);
        }

        foreach (GameObject obj in destroyList)
        {
            Destroy(obj);
        }

        invaders.Clear();

        player = null;
        playerBullet = null;
    }
}