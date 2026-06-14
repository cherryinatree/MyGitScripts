using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalker : MonoBehaviour
{
    public float speed = 4f;
    public float mouseSensitivity = 2f;
    public Transform playerCamera;

    private CharacterController controller;
    private float verticalLookRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        if(controller.enabled)
        controller.Move(move * speed * Time.deltaTime);

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -80f, 80f);
        playerCamera.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }
}
