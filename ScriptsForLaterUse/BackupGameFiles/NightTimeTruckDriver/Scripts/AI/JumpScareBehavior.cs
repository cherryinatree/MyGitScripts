using UnityEngine;

public class JumpScareBehavior : MonoBehaviour, IMonsterBehavior
{
    public AudioClip scareSound;
    public Animator animator;
    private Transform jumpScarePoint; // Where the monster appears for the jump scare

    


    public void Start()
    {
        jumpScarePoint = GameObject.Find("PassengerSeat").transform; // Assuming the jump scare point is the passenger seat of the truck
    }

    public void Execute(MonsterAI monster) 
    {
        if (gameObject.transform.parent != jumpScarePoint)
        {
            gameObject.transform.position = jumpScarePoint.position;
            gameObject.transform.rotation = jumpScarePoint.rotation;
            gameObject.transform.SetParent(jumpScarePoint);
        }
        gameObject.transform.position = jumpScarePoint.position;

    }

        public void OnStateChange(MonsterAI monster, MonsterAI.MonsterState newState)
    {
        if (newState == MonsterAI.MonsterState.Active)
        {
            if (scareSound) AudioSource.PlayClipAtPoint(scareSound, monster.transform.position);
            if (animator) animator.SetTrigger("JumpScare");
        }
    }
}
