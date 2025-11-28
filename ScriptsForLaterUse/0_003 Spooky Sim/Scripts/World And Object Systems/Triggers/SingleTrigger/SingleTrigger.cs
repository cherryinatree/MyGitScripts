using UnityEngine;
using static DirectionGate;

public class SingleTrigger : MonoBehaviour
{
    [Header("General")]
    public string ColliderTag = "Player";
    public bool TriggerOnce = false;
    private bool hasTriggered = false;

    [Header("Repeat")]
    private Timer repeatDelay;
    public bool StayRepeatDelay = false;
    public float StayTriggerRepeatDelay = 0.5f;

    [Header("Events")]
    public PlayerEvent OnEnter;
    public PlayerEvent OnStay;
    public PlayerEvent OnExit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        repeatDelay = new Timer(StayTriggerRepeatDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ColliderTag))
        {
            if (!TriggerOnce)
            {

               OnEnter?.Invoke(other.gameObject);
                return;
            }
            else
            {
                if (!hasTriggered)
                {
                    OnEnter?.Invoke(other.gameObject);
                    hasTriggered = true;
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(ColliderTag))
        {
            if (!TriggerOnce)
            {
                if (StayRepeatDelay)
                {
                    if (repeatDelay.ClockTick())
                    {
                        OnStay?.Invoke(other.gameObject);
                        repeatDelay.RestartTimer();
                        return;
                    }
                }
                else
                {
                    OnStay?.Invoke(other.gameObject);
                }
                return;
            }
            else
            {
                if (!hasTriggered)
                {
                    OnStay?.Invoke(other.gameObject);
                    hasTriggered = true;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ColliderTag))
        {
            if (!TriggerOnce)
            {

                OnExit?.Invoke(other.gameObject);
                return;
            }
            else
            {
                if (!hasTriggered)
                {
                    OnExit?.Invoke(other.gameObject);
                    hasTriggered = true;
                }
            }
        }
    }
}
