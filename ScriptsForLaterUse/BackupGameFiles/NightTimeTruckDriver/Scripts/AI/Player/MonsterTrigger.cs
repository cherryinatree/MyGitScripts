using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MonsterTrigger : MonoBehaviour
{
    public MonsterAI monster;
/*
    private void OnTriggerEnter(Collider other)
    {
        if (PlayerPresence.CurrentPlayerTransform != null &&
            other.transform == PlayerPresence.CurrentPlayerTransform)
        {
            monster.ActivateMonster();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (PlayerPresence.CurrentPlayerTransform != null &&
            other.transform == PlayerPresence.CurrentPlayerTransform)
        {
            monster.DeactivateMonster();
        }
    }*/
}
