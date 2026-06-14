using UnityEngine;

public class TruckStats : MonoBehaviour
{
    [Header("Truck Stats")]
    public float gas = 100f; // percentage
    public float hunger = 100f; // percentage
    public float sanity = 100f; // percentage
    public float speed = 0f; // mph
    public float rpm = 0f;
    public int gear = 1;

    [Header("Delivery Stats")]
    public float distanceTraveled = 0f; // miles
    public float distanceToObjective = 50f; // miles
    public float payRate = 2.5f; // dollars per mile
    public float bonusPerMile = 1.0f; // dollars per mile
    public float money = 0f;

    [Header("Vehicle References")]
    public Rigidbody truckRigidbody;

    private Vector3 lastPosition;

    void Start()
    {
        if (truckRigidbody == null)
        {

            truckRigidbody = GameObject.Find("Truck").GetComponent<Rigidbody>();
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        UpdateStats();
    }


    void UpdateStats()
    {
        // Update distance
        float moved = Vector3.Distance(transform.position, lastPosition);
        distanceTraveled += moved / 1609.34f; // Convert meters to miles
        distanceToObjective = Mathf.Max(0f, distanceToObjective - (moved / 1609.34f));
        lastPosition = transform.position;

        // Update speed
        speed = truckRigidbody.linearVelocity.magnitude * 2.23694f; // m/s to mph

        // Estimate RPM (simplified)
        rpm = speed * gear * 50f;

        // Money based on distance
        money = distanceTraveled * (payRate + bonusPerMile);

        // Decrease gas and hunger slowly
        gas = Mathf.Max(0f, gas - Time.deltaTime * 0.05f); // adjustable
        hunger = Mathf.Max(0f, hunger - Time.deltaTime * 0.01f); // adjustable
        sanity = Mathf.Max(0f, sanity - Time.deltaTime * 0.005f); // adjustable
        SaveSingleton.Instance.truckStats.sanity = sanity; // Save sanity to singleton
        SaveSingleton.Instance.truckStats.hunger = hunger; // Save hunger to singleton
    }

    public void ChangeGear(int newGear)
    {
        gear = Mathf.Clamp(newGear, 1, 18); // Example: 18-wheeler
    }

    public void AddBonus(float amount)
    {
        bonusPerMile += amount;
    }

    public void RefillGas(float amount)
    {
        gas = Mathf.Clamp(gas + amount, 0f, 100f);
    }

    public void EatFood(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
    }

    public void RecoverSanity(float amount)
    {
        sanity = Mathf.Clamp(sanity + amount, 0f, 100f);
    }
}