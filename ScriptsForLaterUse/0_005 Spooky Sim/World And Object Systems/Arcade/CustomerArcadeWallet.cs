using UnityEngine;

public class CustomerArcadeWallet : MonoBehaviour, IArcadeTicketReceiver
{
    public int TicketsWon { get; private set; }

    public void AddTickets(int amount)
    {
        if (amount <= 0)
            return;

        TicketsWon += amount;

        Debug.Log($"{name} won {amount} arcade tickets.");
    }
}
