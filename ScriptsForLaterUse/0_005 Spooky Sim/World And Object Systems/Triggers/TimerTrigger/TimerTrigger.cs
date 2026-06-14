using UnityEngine;
public class TimerTrigger : MonoBehaviour
{
    [Header("Timer Settings")]
    Timer timer;
    public float timeToSet = 5f;
    public bool runAtStart = true;
    public bool repeat = false;

    [Header("Events")]
    public PlayerEvent triggerEvent;
    private bool hasTriggered = false;
    private bool startTimer = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = new Timer(timeToSet);
        if (runAtStart)
        {
            StartTimer();
        }
    }

    // Update is called once per frame
    void Update()
    {
        RunTimer();
    }

    private void RunTimer()
    {

        if (startTimer && !hasTriggered)
        {
            if (timer.ClockTick())
            {
                triggerEvent?.Invoke(this.gameObject);
                hasTriggered = true;
                if (repeat)
                {
                    timer.RestartTimer();
                    hasTriggered = false;
                }
            }
        }
    }

    public void StartTimer()
    {
        startTimer = true;
        hasTriggered = false;
        timer.RestartTimer();
    }
}
