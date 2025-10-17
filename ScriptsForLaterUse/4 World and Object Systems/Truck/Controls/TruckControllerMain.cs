using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TruckControllerMain : MonoBehaviour
{
    public ShiftMiniGameUI shiftMiniGameUI;
    public AudioSource gearGrindSound;

    private bool isMiniGameActive = false;
    private bool isMiniGameComplete = false;

    public float acceleration = 10f;
    public float turnSpeed = 50f;
    public float maxSpeed = 20f;
    public float brakeForce = 0.95f;

    private Rigidbody rb;
    private GameControls controls;

    private Vector2 driveInput;
    private bool isBraking;

    public float maxSpeedMph = 80f;
    public int maxGear = 12;
    public float[] gearRatios; // Length = maxGear, values from low to high

    public float engineRpm; // Exposed for UI
    public float maxRpm = 6000f;
    public float shiftMinRpm = 1500f;
    public float shiftMaxRpm = 3000f;

    public float accelerationForce = 500f;
    public float turnStrength = 2f;

    private float currentSpeed;
    private int currentGear = 1;

    private bool clutchPressed => Input.GetKey(KeyCode.LeftShift);
    private bool canShift => clutchPressed && engineRpm >= shiftMinRpm && engineRpm <= shiftMaxRpm;

    private bool isDriving = true;
    PlayerExitController playerExitController;
    VehicleControls vehicleControls;

    public float grip = 1f; // 0 = no grip, 1 = perfect grip

    private float internalSpeed = 1f; // For internal speed calculations
    private float[] maxSpeedPerGear = {
    5f, 10f, 15f, 20f, 28f, 35f, 42f, 50f, 58f, 66f, 73f, 80f
};

    void Awake()
    {
        controls = new GameControls();

        // Input callbacks
        controls.Truck.Drive.performed += ctx => driveInput = ctx.ReadValue<Vector2>();
        controls.Truck.Drive.canceled += _ => driveInput = Vector2.zero;

        controls.Truck.Brake.performed += _ => isBraking = true;
        controls.Truck.Brake.canceled += _ => isBraking = false;
        controls.Enable();
        playerExitController = GetComponent<PlayerExitController>();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        Time.timeScale = 1f; // Ensure time scale is normal
        rb = GameObject.Find("Truck").GetComponent<Rigidbody>();
        gearGrindSound = GameObject.Find("Truck").GetComponent<AudioSource>();
        rb.centerOfMass = new Vector3(0, -1.0f, 0); // better handling

        rb.linearDamping = 0.05f; // Lower = slower deceleration
        rb.angularDamping = 0.05f;


        vehicleControls = GetComponent<VehicleControls>();
        vehicleControls.VC_Start(rb);


        if (gearRatios == null || gearRatios.Length != maxGear)
        {
            gearRatios = new float[maxGear];
            for (int i = 0; i < maxGear; i++)
                gearRatios[i] = 2f + i * 0.5f; // simple curve
        }
    }

    void FixedUpdate()
    {
        isDriving = playerExitController.isDriving;

        if (isDriving)
        {
            /*  currentSpeed = rb.linearVelocity.magnitude;
              CalculateRPM();

              HandleDriving();
              HandleTurning();

              // Apply grip (realistic direction change)
              Vector3 forward = transform.forward.normalized;
              Vector3 velocity = rb.linearVelocity;
              Vector3 projected = Vector3.Project(velocity, forward);
              Vector3 correctedVelocity = Vector3.Lerp(velocity, projected, grip);
              rb.linearVelocity = correctedVelocity;*/
            //vehicleControls.VC_FixedUpdate();
        }
        else
        {
            Debug.Log("Truck is not driving");
        }
    }

    void Update()
    {
        if (isDriving)
        {
            HandleShiftingInput();
            CalculateGasUsed();
            vehicleControls.VC_FixedUpdate();
        }
    }

    void CalculateGasUsed()
    {

        SaveSingleton.Instance.truckStats.gas = Mathf.Max(0f, SaveSingleton.Instance.truckStats.gas - Time.deltaTime * 0.0005f * engineRpm); // adjustable
    }

    void CalculateRPM()
    {
        float wheelRPM = (currentSpeed / (2 * Mathf.PI)) * 60f; // convert m/s to RPM
        engineRpm = wheelRPM * gearRatios[currentGear - 1];
        engineRpm = Mathf.Clamp(engineRpm, 500f, maxRpm);
    }

    void HandleDriving()
    {
        float input = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);

        if (input == 0f || clutchPressed)
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 resistance = velocity.normalized * accelerationForce * gearRatios[currentGear - 1];
            float force = accelerationForce * gearRatios[currentGear - 1];
            internalSpeed = internalSpeed - (Time.fixedDeltaTime / 5);
            if (internalSpeed < 0)
            {
                internalSpeed = 0;
            }
            //Debug.Log("Resistance Force: " + resistance);
            rb.AddForce(rb.transform.forward * force * Time.fixedDeltaTime * internalSpeed, ForceMode.Acceleration);
        }
        // Only apply force if clutch is not pressed
        if (!clutchPressed)
        {
            float force = input * accelerationForce * gearRatios[currentGear - 1];
            rb.AddForce(rb.transform.forward * force * Time.fixedDeltaTime, ForceMode.Acceleration);
            if (force != 0)
            {
                internalSpeed = 1;
            }
        }


        float currentSpeed = rb.linearVelocity.magnitude * 2.23694f; // Convert m/s to mph
        float maxSpeedForCurrentGear = maxSpeedPerGear[currentGear - 1]; // Assuming gear starts at 1

        float speedNormalized = Mathf.Clamp01(currentSpeed / maxSpeedForCurrentGear);

        if (speedNormalized >= 1f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeedForCurrentGear * 0.44704f; // Convert mph to m/s
        }
    }

    void HandleTurning()
    {
        float turn = 0f;
        if (Input.GetKey(KeyCode.A)) turn = -1f;
        if (Input.GetKey(KeyCode.D)) turn = 1f;

        float turnFactor = Mathf.Clamp01(currentSpeed / 5f);
        rb.gameObject.transform.Rotate(0f, turn * turnStrength * turnFactor, 0f);
    }

    void HandleShiftingInput()
    {

        if (!clutchPressed && isMiniGameComplete)
        {
            isMiniGameComplete = false;
        }
        if (!clutchPressed && isMiniGameActive)
        {
            isMiniGameActive = false;
        }

        if (!clutchPressed || isMiniGameComplete)
        {
            return;
        }


        if (!isMiniGameActive)
        {
            shiftMiniGameUI.StartMiniGame();
        }
        isMiniGameActive = true;
        shiftMiniGameUI.MoveMarker();
        if (Input.GetKeyDown(KeyCode.R) && currentGear < maxGear)
            TryStartShift(true);

        if (Input.GetKeyDown(KeyCode.F) && currentGear > 1)
            TryStartShift(false);
    }

    void TryStartShift(bool up)
    {
        isMiniGameActive = false;
        isMiniGameComplete = true;

        if (shiftMiniGameUI.ChangeGears())
        {
            currentGear += up ? 1 : -1;

        }
        else
        {
            if (gearGrindSound) gearGrindSound.Play();
        }
    }

    public int GetCurrentGear() => currentGear;
    public float GetCurrentSpeedMph() => rb.linearVelocity.magnitude / 0.44704f;
    public float GetCurrentRPM() => engineRpm;
}