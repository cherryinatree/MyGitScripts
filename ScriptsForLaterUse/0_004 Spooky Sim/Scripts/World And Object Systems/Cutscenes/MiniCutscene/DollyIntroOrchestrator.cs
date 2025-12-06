// DollyIntroOrchestratorCM3.cs
using UnityEngine;
using System.Collections;
using Unity.Cinemachine; // CM3 namespace
using UnityEngine.Splines;   // <-- PathIndexUnit lives here

public class DollyIntroOrchestrator : MonoBehaviour
{
    [Header("Player (re-activated at end)")]
    public GameObject playerRoot;          // whole player object
    public Transform playerCamera;         // the player's Camera transform (child of playerRoot)
    public Transform PlayerCameraTranformParent;
    public Vector3 playercameraHeightOffset = new Vector3(0, 0, 0);
    public CharacterController playerCC;   // optional

    [Header("Cutscene Camera Rig (active during intro)")]
    public GameObject cutscenePhysicalCamera;   // Camera with CinemachineBrain (kept active during intro)
    public CinemachineCamera dollyCmCamera;     // CinemachineCamera (CM3)
    public CinemachineSplineDolly splineDolly;  // Body component on the CinemachineCamera

    [Header("Timing")]
    [Min(0f)] public float holdSeconds = 2f;
    [Min(0.01f)] public float travelSeconds = 2f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Spline Position Units")]
    public PathIndexUnit positionUnits = PathIndexUnit.Normalized; // 0..1 by default
    public float startPos = 0f;
    public float endPos = 1f;

    // cache the player camera's local pose relative to the player root
    Vector3 _camLocalPos;
    Quaternion _camLocalRot;

    Vector3 playerOrigionalPosition;

    void Awake()
    {
        if (!playerRoot || !playerCamera || !cutscenePhysicalCamera || !dollyCmCamera)
            Debug.LogError($"{name}: Assign Player Root/Camera, Cutscene Camera, and CinemachineCamera.");
        if (!splineDolly) splineDolly = dollyCmCamera.GetComponent<CinemachineSplineDolly>();
        _camLocalPos = playerCamera.localPosition;
        _camLocalRot = playerCamera.localRotation;
    }

    void OnEnable() => StartCoroutine(Run());

    IEnumerator Run()
    {
        yield return PrewarmPlayerForStart(playerRoot, playerCamera, new Behaviour[] {
    /* e.g. your movement script or PlayerInput if you don't want it to tick this frame */
        });
        // Activate cutscene, deactivate player
        if (playerRoot) playerRoot.SetActive(false);
        if (cutscenePhysicalCamera) cutscenePhysicalCamera.SetActive(true);

        // Put camera at start and hold
        SetCamPos(startPos);
        if (holdSeconds > 0f) yield return new WaitForSeconds(holdSeconds);

        // Move along spline with easing
        float t = 0f;
        while (t < travelSeconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / travelSeconds);
            float k = ease != null ? Mathf.Clamp01(ease.Evaluate(a)) : a;
            SetCamPos(Mathf.Lerp(startPos, endPos, k));
            yield return null;
        }
        SetCamPos(endPos);

        // Match player rig to the *current* rendered camera pose (the Brain-controlled physical camera)
        var liveCam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (liveCam) SnapPlayerToMatch(liveCam.transform.position, liveCam.transform.rotation);

        // Swap: disable cutscene, enable player
        if (cutscenePhysicalCamera) cutscenePhysicalCamera.SetActive(false);
        if (playerRoot)
        {
            playerRoot.SetActive(true);
            playerCamera.position = PlayerCameraTranformParent.position + playercameraHeightOffset;
        }

    }

    IEnumerator PrewarmPlayerForStart(GameObject playerRoot, Transform playerCam, Behaviour[] toMute)
    {
        if (!playerRoot) yield break;

        // Mute visuals & input while we prewarm
        var cam = playerCam ? playerCam.GetComponent<Camera>() : null;
        var listener = playerCam ? playerCam.GetComponent<AudioListener>() : null;
        bool camWas = cam && cam.enabled; bool lisWas = listener && listener.enabled;
        if (cam) cam.enabled = false; if (listener) listener.enabled = false;
        foreach (var b in toMute) if (b) b.enabled = false;

        // Activate -> Awake runs immediately; Start will run on the next frame **while active**
        playerRoot.SetActive(true);
        yield return null; // allow Start() to execute

        // Deactivate again until handoff time
        playerRoot.SetActive(false);

        // Restore muted components
        foreach (var b in toMute) if (b) b.enabled = true;
        if (cam) cam.enabled = camWas; if (listener) listener.enabled = lisWas;
    }
    private void MovePlayer()
    {
       // playerOrigionalPosition = playerRoot.transform.position;
        //playerRoot.transform.position = new Vector3(0, -1000, 0);
    }

    private void MovePlayerToOrigionalPosition()
    {
        //playerRoot.transform.position = cutscenePhysicalCamera.transform.position;
        //cutscenePhysicalCamera.transform.position = playerRoot.transform.position;
        //playerCamera.transform.position = playerRoot.transform.position;
    }


    void SetCamPos(float p)
    {
        if (!splineDolly) return;
        splineDolly.PositionUnits = positionUnits;
        splineDolly.CameraPosition = p;   // CM3 property to set along-spline position
        // Tip: You can also keyframe CameraPosition in Timeline if preferred. :contentReference[oaicite:1]{index=1}
    }

    void SnapPlayerToMatch(Vector3 desiredCamPos, Quaternion desiredCamRot)
    {
        if (!playerRoot || !playerCamera) return;

        bool ccWasEnabled = playerCC && playerCC.enabled;
        if (ccWasEnabled) playerCC.enabled = false;

        // Solve root so that: root * (playerCamera local) == desired camera world
        Quaternion rootRot = desiredCamRot * Quaternion.Inverse(_camLocalRot);
        Vector3 rootPos = desiredCamPos - (rootRot * _camLocalPos);

        //playerRoot.transform.SetPositionAndRotation(rootPos, rootRot);
        Physics.SyncTransforms();

        if (ccWasEnabled) playerCC.enabled = true;
    }
}
