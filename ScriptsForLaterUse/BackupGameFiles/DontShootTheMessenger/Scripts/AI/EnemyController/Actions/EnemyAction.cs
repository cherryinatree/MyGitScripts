using UnityEngine;

public abstract class EnemyAction : MonoBehaviour
{
    protected CoreEnemy core;

    protected virtual void Awake()
    {
        core = GetComponentInParent<CoreEnemy>();
    }
}
