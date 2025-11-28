using UnityEngine;

public class MonsterGrabber : MonoBehaviour
{
    [Header("Grab Settings")]
    public Transform grabPoint;               // child near face/hands
    public Transform lookAtPoint;               // child near face/hands
    public float grabRange = 1.6f;
    public LayerMask playerMask;

    [Header("Timing")]
    public float grabLoopMin = 1.0f;
    public float grabLoopMax = 2.5f;

    [Header("Throw Settings")]
    public float throwUp = 3.5f;
    public float throwForward = 7.5f;

    [Header("Animator")]
    public Animator animator;
    public string grabTrigger = "Grab";
    public string grabLoopBool = "IsHolding";
    public string throwTrigger = "Throw";
    public string killTrigger = "Kill";

    PlayerGrabbedHandler _player;
    float _loopEndTime;

    Timer cooldown;
    public float cooldownTimer = 10f;

    private void OnEnable()
    {
        cooldown = new Timer(0);
    }

    void Update()
    {
        if (_player != null && _player.IsGrabbed)
        {
            // already grabbing, wait for loop end
            if (Time.time >= _loopEndTime)
            {
                // choose ending
                bool doThrow = Random.value > 0.5f;
                Debug.Log("Throw");
                cooldown.NewStopTime(cooldownTimer);
                cooldown.RestartTimer();
                if (doThrow) 
                {
                    Anim_ThrowPlayer();
                    animator.SetTrigger(throwTrigger);
                }
                else
                {
                    Anim_ThrowPlayer();
                    animator.SetTrigger(killTrigger); 
                }
            }
            return;
        }
        if(!cooldown.ClockTick())
            return;

        // Find player in range
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRange, playerMask);
        Debug.Log($"MonsterGrabber: Found {hits.Length} players in range.");
        if (hits.Length > 0)
        {
            var p = hits[0].GetComponentInParent<PlayerGrabbedHandler>();

            if (p != null)
            {
                _player = p;
                Anim_BeginHold();
                animator.SetTrigger(grabTrigger);
            }
        }
    }

    // ---- Animation Event Hooks ----

    // Call this at the moment hands connect (end of GrabStart)
    public void Anim_BeginHold()
    {
        if (_player == null) return;

        _player.BeginGrab(grabPoint, lookAtPoint);
        animator.SetBool(grabLoopBool, true);
        _loopEndTime = Time.time + Random.Range(grabLoopMin, grabLoopMax);
    }

    // Call at the start of GrabThrow animation right when you want the player released.
    public void Anim_ThrowPlayer()
    {
        if (_player == null) return;

        animator.SetBool(grabLoopBool, false);

        Vector3 vel = transform.forward * throwForward + Vector3.up * throwUp;
        _player.EndGrabThrow(vel);

        _player = null;
    }

    // Call at the kill “snap” moment.
    public void Anim_KillPlayer()
    {
        if (_player == null) return;

        animator.SetBool(grabLoopBool, false);
        _player.EndGrabKill();

        _player = null;
    }
}
