using UnityEngine;
using UnityEngine.Events;

public class ArcadeTicketDispenser : MonoBehaviour
{
    [Header("Ticket Strip")]
    [SerializeField] private ArcadeTicketStripCollectible ticketStripPrefab;
    [SerializeField] private Transform ticketSpawnPoint;

    [Header("Default Player Inventory")]
    [SerializeField] private ArcadeTicketInventory playerTicketInventory;

    [Header("Events")]
    public UnityEvent<int> onTicketsDispensed;
    public UnityEvent<int> onTicketsCollected;

    private ArcadeTicketStripCollectible activeStrip;

    private void Awake()
    {
        if (playerTicketInventory == null)
            playerTicketInventory = ArcadeTicketInventory.Instance;
    }

    public void DispenseTicketsForPlayer(int amount)
    {
        DispenseTickets(
            amount,
            ArcadeTicketPayoutMode.PlayerCollectible,
            playerTicketInventory);
    }

    public void DispenseTicketsToReceiver(
        int amount,
        IArcadeTicketReceiver receiver)
    {
        DispenseTickets(
            amount,
            ArcadeTicketPayoutMode.DirectToCustomer,
            receiver);
    }

    public void DispenseTickets(
        int amount,
        ArcadeTicketPayoutMode payoutMode,
        IArcadeTicketReceiver receiver = null)
    {
        if (amount <= 0)
            return;

        onTicketsDispensed?.Invoke(amount);

        switch (payoutMode)
        {
            case ArcadeTicketPayoutMode.PlayerCollectible:
                PrintCollectibleTicketStrip(amount);
                break;

            case ArcadeTicketPayoutMode.DirectToPlayerInventory:
                if (playerTicketInventory == null)
                    playerTicketInventory = ArcadeTicketInventory.Instance;

                playerTicketInventory?.AddTickets(amount);
                break;

            case ArcadeTicketPayoutMode.DirectToCustomer:
                receiver?.AddTickets(amount);
                break;

            case ArcadeTicketPayoutMode.None:
                break;
        }
    }

    private void PrintCollectibleTicketStrip(int amount)
    {
        if (ticketStripPrefab == null)
        {
            Debug.LogWarning("ArcadeTicketDispenser has no ticket strip prefab assigned.");
            return;
        }

        if (ticketSpawnPoint == null)
        {
            Debug.LogWarning("ArcadeTicketDispenser has no ticket spawn point assigned.");
            return;
        }

        if (playerTicketInventory == null)
            playerTicketInventory = ArcadeTicketInventory.Instance;

        if (activeStrip != null)
        {
            activeStrip.AddMoreTickets(amount);
            return;
        }

        activeStrip = Instantiate(
            ticketStripPrefab,
            ticketSpawnPoint.position,
            ticketSpawnPoint.rotation, ticketSpawnPoint);

        activeStrip.Initialize(
            amount,
            playerTicketInventory,
            this);
    }

    public void NotifyTicketsCollected(
        ArcadeTicketStripCollectible strip,
        int amount)
    {
        if (activeStrip == strip)
            activeStrip = null;

        onTicketsCollected?.Invoke(amount);
    }
}