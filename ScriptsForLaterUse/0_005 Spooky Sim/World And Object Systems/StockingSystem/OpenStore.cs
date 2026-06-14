using Cherry.Gameplay;
using NUnit.Framework;
using UnityEngine;

public class OpenStore : MonoBehaviour
{

    public Animator EntranceAnimator;

    private Timer randomAnimationTimer;
    public float randomAnimationInterval = 60f;


    public GameObject openStoreSign;
    public GameObject portal;

    public Material openMaterial;
    public Material closedMaterial;

    public GameObject[] customers;
    public Transform customerSpawnLocation;

    private Timer spawnTimer;
    public float spawnInterval = 30f;



    private bool isOpen = false;

    public DailyActionGate openGate;
    public DailyActionGate closeGate;

    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        openStoreSign.GetComponent<Renderer>().material = closedMaterial;
        portal.SetActive(false);
        spawnTimer = new Timer(spawnInterval);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        randomAnimationTimer = new Timer(randomAnimationInterval);
    }

    private void Update()
    {
        SpawnCustomers();

        if (randomAnimationTimer.ClockTick())
        {
            if(isOpen)
            EntranceAnimator.SetTrigger("LookAroundOn");
            else EntranceAnimator.SetTrigger("LookAroundOff");

            randomAnimationTimer.RestartTimer();
        }
    }


    private void SpawnCustomers()
    {
        if (customers.Length == 0) return;
        if (!isOpen) return;
        if (spawnTimer.ClockTick())
        {
            int randomIndex = Random.Range(0, customers.Length-1);
            GameObject customer = Instantiate(customers[randomIndex], customerSpawnLocation.position, customerSpawnLocation.rotation);
            spawnTimer.RestartTimer();
        }
    }

    public void ToggleStore()
    {
        Debug.Log("Toggling Store Open/Closed");
        if(closeGate.CanDoToday() && isOpen)
        {
            closeGate.TryDoToday();

            EntranceAnimator.SetTrigger("TurnOff");
            randomAnimationTimer.RestartTimer();

            if (audioSource != null && closeSound != null)
                audioSource.PlayOneShot(closeSound);
        }
        else if(openGate.CanDoToday() && !isOpen)
        {
            openGate.TryDoToday();

            EntranceAnimator.SetTrigger("TurnOn");
            randomAnimationTimer.RestartTimer();

            if (audioSource != null && closeSound != null)
                audioSource.PlayOneShot(openSound);
        }
        else
        {
            Debug.Log("Store cannot be toggled at this time.");
            return;
        }

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
