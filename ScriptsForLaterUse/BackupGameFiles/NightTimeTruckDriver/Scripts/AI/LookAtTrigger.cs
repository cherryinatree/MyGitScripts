using UnityEngine;

public class LookAtTrigger : MonoBehaviour, IMonsterTrigger
{
    public float fovAngle = 45f;
    private Transform playerCamera;

    void Start()
    {
        playerCamera = Camera.main.transform;
    }

    public bool ShouldActivate(MonsterAI monster)
    {
        Vector3 dirToMonster = (monster.transform.position - playerCamera.position).normalized;
        float dot = Vector3.Dot(playerCamera.forward, dirToMonster);
        return dot > Mathf.Cos(fovAngle * Mathf.Deg2Rad / 2);
    }
}
