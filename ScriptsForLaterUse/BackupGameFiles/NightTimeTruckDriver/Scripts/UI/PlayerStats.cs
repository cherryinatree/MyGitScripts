using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float gas = 100f;
    public float money = 250.00f;
    public float hunger = 50f;
    public float sanity = 100f;

    public float payPerMile = 5f;
    public float bonusPerMile = 2f;

    void Update()
    {
        // Example stat decay
        hunger -= Time.deltaTime * 0.01f;
        sanity -= Time.deltaTime * 0.005f;
        gas -= Time.deltaTime * 0.05f;
    }
}