using UnityEngine;

public abstract class ArcadeGame : MonoBehaviour
{
    protected int score;

    public virtual void StartGame()
    {
        score = 0;
        gameObject.SetActive(true);
    }

    public virtual void EndGame()
    {
        int tickets = CalculateTickets();

        TicketManager.Instance.AddTickets(tickets);

        gameObject.SetActive(false);

        ArcadeUIManager.Instance.CloseGame();
    }

    protected virtual int CalculateTickets()
    {
        return score / 10;
    }
}