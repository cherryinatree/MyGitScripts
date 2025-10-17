using MoreMountains.CorgiEngine;
using UnityEngine;
using System.Collections.Generic;

public static class ColliderCheck
{


    public static List<GameObject> SpawnMeleeOnePerson(Vector3 origionalPosition, Vector3 offSet, float comboAttackRadius)
    {
        Vector3 meleeSpawnPosition = origionalPosition + offSet;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(meleeSpawnPosition, comboAttackRadius);
        List<GameObject> enemiesHit = new List<GameObject>();

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.tag == "Enemy")
            {
                enemiesHit.Add(enemy.gameObject);
            }
        }
        return enemiesHit;
    }
}
