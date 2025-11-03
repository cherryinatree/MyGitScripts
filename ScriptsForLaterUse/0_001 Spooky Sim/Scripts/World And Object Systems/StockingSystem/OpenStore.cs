using NUnit.Framework;
using UnityEngine;

public class OpenStore : MonoBehaviour
{

    public GameObject openStoreSign;
    public GameObject portal;

    public Material openMaterial;
    public Material closedMaterial;

    public GameObject[] customers;
    public Transform customerSpawnLocation;

    private Timer spawnTimer;
    public float spawnInterval = 30f;



    private bool isOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        openStoreSign.GetComponent<Renderer>().material = closedMaterial;
        portal.SetActive(false);
        spawnTimer = new Timer(spawnInterval);
    }

    private void Update()
    {
        SpawnCustomers();
    }


    private void SpawnCustomers()
    {
        if (customers.Length == 0) return;
        if (!isOpen) return;
        if (spawnTimer.ClockTick())
        {
            Debug.Log("Spawn Customer");
            int randomIndex = Random.Range(0, customers.Length-1);
            GameObject customer = Instantiate(customers[randomIndex], customerSpawnLocation.position, customerSpawnLocation.rotation);
            spawnTimer.RestartTimer();
        }
    }

    public void ToggleStore()
    {
        Debug.Log("Toggling Store Open/Closed");
        isOpen = !isOpen;

        if (isOpen)
        {
            openStoreSign.GetComponent<Renderer>().material = openMaterial;
            portal.SetActive(true);
        }
        else
        {
            openStoreSign.GetComponent<Renderer>().material = closedMaterial;
            portal.SetActive(false);
        }
    }
}
