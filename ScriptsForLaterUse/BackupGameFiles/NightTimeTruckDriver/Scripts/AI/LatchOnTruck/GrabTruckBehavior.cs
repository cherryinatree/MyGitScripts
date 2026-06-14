using UnityEngine;

public class GrabTruckBehavior : MonoBehaviour, IMonsterBehavior
{
    public Transform grabPoint;              // Where monster latches (assign truck mount point in prefab/scene)
    public Animator animator;                // Monster animation
    public AudioClip grabSound;              // Sound when latching
    public float breakInTime = 10f;          // Time until monster breaks in
    public bool repelled = false;            // Whether player scared it away

    private float timer;
    private bool isGrabbing = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Execute(MonsterAI monster)
    {
        if (monster.currentState == MonsterAI.MonsterState.Active && !isGrabbing)
        {
            // Latch onto the truck
            if (grabPoint != null)
            {
                transform.SetParent(grabPoint);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            if (grabSound) audioSource.PlayOneShot(grabSound);
            if (animator) animator.SetTrigger("Grab");

            isGrabbing = true;
            timer = breakInTime;
        }

        // If grabbing, count down to breaking in
        if (isGrabbing && !repelled)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                BreakIn(monster);
            }
        }

        // If repelled, despawn
        if (repelled && isGrabbing)
        {
            Repelled(monster);
        }
    }

    public void OnStateChange(MonsterAI monster, MonsterAI.MonsterState newState)
    {
        if (newState == MonsterAI.MonsterState.Despawned && isGrabbing)
        {
            transform.SetParent(null);
        }
    }

    private void BreakIn(MonsterAI monster)
    {
        Debug.Log("Monster broke into the truck!");
        if (animator) animator.SetTrigger("BreakIn");

        // TODO: damage player, trigger game over, etc.
        monster.Despawn();
    }

    public void Repelled(MonsterAI monster)
    {
        Debug.Log("Monster repelled!");
        if (animator) animator.SetTrigger("RunAway");

        transform.SetParent(null);
        monster.Despawn();
    }

    // Call this externally from horn/light/etc.
    public void SetRepelled()
    {
        repelled = true;
    }
}
