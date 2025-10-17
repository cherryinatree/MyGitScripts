using UnityEngine;

public class PlayerExitController : MonoBehaviour
{
    public GameObject truckPlayerModel;     // The character inside the truck (e.g. driver arms, body, etc.)
    public GameObject walkingPlayer;        // The FPS walking character (capsule with camera and controller)
    public GameObject truckModel;
    public KeyCode exitKey = KeyCode.F;
    Transform playerSpawnOutsideTruck;

    public bool isDriving = true;

    public void Start()
    {
        truckModel = GameObject.Find("Truck");
        //truckPlayerModel = truckModel.transform.Find("PlayerModel1").gameObject;
        walkingPlayer = truckModel.transform.Find("PlayerWalkingModel1").gameObject;
        playerSpawnOutsideTruck = truckModel.transform.Find("PlayerSpawnOutOfTruck");

        //truckPlayerModel.SetActive(false);
        walkingPlayer.SetActive(true);
        walkingPlayer.GetComponent<CharacterController>().enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(exitKey))
        {
            if (isDriving)
                ExitTruck();
            else
                EnterTruck();
        }
    }

    void ExitTruck()
    {
        isDriving = false;

        // Switch cameras and models
        //truckPlayerModel.SetActive(false);

        walkingPlayer.transform.position = GameObject.Find("Truck").transform.Find("PlayerSpawnOutOfTruck").position;
        //walkingPlayer.SetActive(true);

        walkingPlayer.GetComponent<CharacterController>().enabled = true;

        UnlockCursor();
    }

    void EnterTruck()
    {
        isDriving = true;

        // Move player into truck and disable walking player
        //truckPlayerModel.transform.position = walkingPlayer.transform.position;
        //walkingPlayer.transform.position = playerSpawnOutsideTruck.position;
        // walkingPlayer.SetActive(false);

        // truckPlayerModel.SetActive(true);

        walkingPlayer.transform.position = GameObject.Find("Truck").transform.Find("PlayerSpawnInTruck").position;
        walkingPlayer.GetComponent<CharacterController>().enabled = false;

        LockCursor();
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
