using TMPro;
using UnityEngine;

public class TicketManager : MonoBehaviour
{
    public static TicketManager Instance;

    public int Tickets { get; private set; }

    [SerializeField] private TMP_Text ticketText;

    private void Awake()
    {
        Instance = this;
    }

    public void AddTickets(int amount)
    {
        Tickets += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (ticketText != null)
            ticketText.text = $"Tickets: {Tickets}";
    }
}