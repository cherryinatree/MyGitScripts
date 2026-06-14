using UnityEngine;

public class RunAwayOnLookBehavior : MonoBehaviour, IMonsterBehavior
{
    public float speed = 6f;
    private Transform playerCamera;

    void Start()
    {
        playerCamera = Camera.main.transform;
    }

    public void Execute(MonsterAI monster)
    {
        if (monster.currentState == MonsterAI.MonsterState.Active)
        {
            Vector3 dirFromPlayer = (monster.transform.position - playerCamera.position).normalized;
            monster.transform.position += dirFromPlayer * speed * Time.deltaTime;
        }
    }

    public void OnStateChange(MonsterAI monster, MonsterAI.MonsterState newState) { }
}
