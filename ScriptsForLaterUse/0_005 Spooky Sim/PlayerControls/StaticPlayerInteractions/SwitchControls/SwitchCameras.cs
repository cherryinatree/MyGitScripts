using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class SwitchCameras : MonoBehaviour
{

    [SerializeField] private CinemachineBrain playerCamera;
    [SerializeField] private CinemachineVirtualCameraBase uiCamera;

    public GameObject Equipment;

    public int uiCameraPriority = 51;
    private int origionalUiCameraPriority;


    private bool isPlayerCameraActive = true;


    private void Start()
    {
        //playerCamera.gameObject.SetActive(true);
        origionalUiCameraPriority = uiCamera.Priority;
        
    }

    public void FlopScripts()
    {
        Debug.Log("Flopping Cameras");
        if (isPlayerCameraActive)
        {
            //playerCamera.gameObject.SetActive(false);
            //uiCamera.gameObject.SetActive(true);
            uiCamera.Priority = uiCameraPriority;
            isPlayerCameraActive = false;
            Equipment.SetActive(false);
        }
        else
        {
            //playerCamera.gameObject.SetActive(true);
            //uiCamera.gameObject.SetActive(false);
            uiCamera.Priority = origionalUiCameraPriority;
            isPlayerCameraActive = true;
            Equipment.SetActive(true);
        }
    }
}
