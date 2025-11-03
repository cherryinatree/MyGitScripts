using Unity.VisualScripting;
using UnityEngine;

public class SwitchCameras : MonoBehaviour
{

    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera uiCamera;


    private bool isPlayerCameraActive = true;

    private void Start()
    {
        playerCamera.gameObject.SetActive(true);
        uiCamera.gameObject.SetActive(false);
    }

    public void FlopScripts()
    {
        if (isPlayerCameraActive)
        {
            playerCamera.gameObject.SetActive(false);
            uiCamera.gameObject.SetActive(true);
            isPlayerCameraActive = false;
        }
        else
        {
            playerCamera.gameObject.SetActive(true);
            uiCamera.gameObject.SetActive(false);
            isPlayerCameraActive = true;
        }
    }
}
