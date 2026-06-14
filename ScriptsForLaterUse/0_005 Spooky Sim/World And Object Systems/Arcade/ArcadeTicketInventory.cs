using UnityEngine;

public class ArcadeTicketInventory : MonoBehaviour, IArcadeTicketReceiver
{
    public static ArcadeTicketInventory Instance;

    public int Tickets { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void AddTickets(int amount)
    {
        if (amount <= 0)
            return;

        Tickets += amount;
        Debug.Log($"Player received {amount} tickets. Total: {Tickets}");
    }
}