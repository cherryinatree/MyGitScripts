using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int deliveriesToWin = 10;
    public int currentDeliveries = 0;

    [Header("References")]
    public RoundManager roundManager;

    private bool gameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartNewRound();
    }

    public void StartNewRound()
    {
        if (gameOver) return;
        roundManager.StartRound();
    }

    public void CompleteDelivery()
    {
        if (gameOver) return;

        currentDeliveries++;

        HUDController.Instance.IncrementDelivery();
        HUDController.Instance.UpdateObjective("Return to forward base!");
        Debug.Log("Delivery completed! Total: " + currentDeliveries);

        if (currentDeliveries >= deliveriesToWin)
        {
            Victory();
        }
        else
        {
            StartNewRound();
        }
    }

    public void PlayerDied()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("Game Over! You died.");
        // TODO: trigger Game Over UI
    }

    private void Victory()
    {
        gameOver = true;
        Debug.Log("Victory! All targets hit.");
        // TODO: trigger Victory UI
    }
}
