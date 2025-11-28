using UnityEngine;

public class DamagePlayer : MonoBehaviour
{
    public float damageAmount = 10f;
    public void ApplyDamage(GameObject player)
    {
        var playerHealth = player.GetComponent<Cherry.Combat.PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log("Damaging Player");
            playerHealth.TakeDamage(damageAmount);
        }
    }
}
