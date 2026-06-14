using UnityEngine;

public class ProximityTrigger : MonoBehaviour, IMonsterTrigger
{
    public float activationDistance = 30f;
    private Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    public bool ShouldActivate(MonsterAI monster)
    {
        return Vector3.Distance(player.position, monster.transform.position) <= activationDistance;
    }
}
