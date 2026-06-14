using UnityEngine;

public class ChasePlayerBehavior : MonoBehaviour, IMonsterBehavior
{
    public float speed = 5f;
    private Transform player;
    public Animator animator;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        animator = GetComponent<Animator>();
    }

    public void Execute(MonsterAI monster)
    {
        if (monster.currentState == MonsterAI.MonsterState.Active)
        {
            Vector3 dir = (new Vector3(player.position.x, monster.transform.position.y, player.position.z) - monster.transform.position).normalized;
            monster.GetComponent<Rigidbody>().MovePosition(monster.transform.position + (dir * speed * Time.deltaTime));
            monster.transform.LookAt(new Vector3(player.position.x, monster.transform.position.y, player.position.z));
            if (animator) animator.SetBool("Run", true);
        }
    }

    public void OnStateChange(MonsterAI monster, MonsterAI.MonsterState newState) { }
}
