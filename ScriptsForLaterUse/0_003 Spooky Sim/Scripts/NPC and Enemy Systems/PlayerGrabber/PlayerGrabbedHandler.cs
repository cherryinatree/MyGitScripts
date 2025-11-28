using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;
//#if CINEMACHINE
//using Cinemachine;
//#endif

public class PlayerGrabbedHandler : MonoBehaviour
{
    [Header("Anchors")]
    public Transform grabAnchor;        // child on player near head/camera
    public Transform cameraRoot;        // your camera parent / head transform

    [Header("Disable While Grabbed")]
    public MonoBehaviour[] scriptsToDisable;
    public CharacterController characterController; // optional
    public Rigidbody rb;               // optional, if you throw with physics

    [Header("Camera")]
    public CinemachineCamera playerCam;
    public CinemachineCamera grabCam;

    public float cameraYankStrength = 0.25f; // fallback if no cinemachine
    public float cameraYankSpeed = 8f;

    [Header("Events")]
    public UnityEvent onGrabbed;
    public UnityEvent onReleased;
    public UnityEvent onKilled;

    bool _isGrabbed;
    Transform _monsterGrabPoint;
    Transform _monsterLookAtPoint;
    Coroutine _holdRoutine;

    public bool IsGrabbed => _isGrabbed;

    public void BeginGrab(Transform monsterGrabPoint, Transform lookAt)
    {
        if (_isGrabbed) return;
        _isGrabbed = true;
        _monsterGrabPoint = monsterGrabPoint;
        _monsterLookAtPoint = lookAt;
        // Disable gameplay scripts
        foreach (var s in scriptsToDisable)
            if (s) s.enabled = false;

        if (characterController) characterController.enabled = false;
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        //#if CINEMACHINE
        if (grabCam)
        {
            grabCam.Target.TrackingTarget = _monsterLookAtPoint;
            grabCam.Priority = 50;
        }
        if (playerCam) playerCam.Priority = 0;
//#endif

        onGrabbed?.Invoke();

        // Start lock-to-face hold
        _holdRoutine = StartCoroutine(HoldToGrabPoint());
    }

    IEnumerator HoldToGrabPoint()
    {
        // Snap/lock player head to monster grab point
        // We keep the whole player aligned so it feels physical.
        while (_isGrabbed && _monsterGrabPoint)
        {
            // Align player so grabAnchor matches monsterGrabPoint
            Vector3 offset = grabAnchor.position - transform.position;
            Vector3 targetPos = _monsterGrabPoint.position - offset;

            transform.position = Vector3.Lerp(transform.position, _monsterGrabPoint.position, Time.deltaTime * 12f);

            //transform.position = _monsterGrabPoint.position;
            // Face monster (optional but sells it)
            /*Vector3 lookDir = (_monsterGrabPoint.position - cameraRoot.position);
            lookDir.y = 0;
            Debug.Log(lookDir);
            if (lookDir.sqrMagnitude > 0.0001f)
                cameraRoot.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);*/

//#if !CINEMACHINE
            // fallback: yank camera a little toward grab point
            if (cameraRoot)
           {
              //  Vector3 camTarget = Vector3.Lerp(cameraRoot.position, _monsterGrabPoint.position, cameraYankStrength);
              //  cameraRoot.position = Vector3.Lerp(cameraRoot.position, camTarget, Time.deltaTime * cameraYankSpeed);
            }
//#endif
            yield return null;
        }
    }

    public void EndGrabThrow(Vector3 throwVelocity, float reenableDelay = 0.2f)
    {
        if (!_isGrabbed) return;
        _isGrabbed = false;

        if (_holdRoutine != null) StopCoroutine(_holdRoutine);

        //#if CINEMACHINE
        if (grabCam)
        {
            grabCam.Priority = 0;
        }
        if (playerCam)
        {
            transform.rotation = new Quaternion(transform.rotation.x, grabCam.transform.rotation.y,
                transform.rotation.z, transform.rotation.w);

            playerCam.transform.rotation = new Quaternion(0, 0, 0, 0);
            playerCam.Priority = 50;
        }
//#endif

        // Re-enable controller/physics to throw
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = throwVelocity;
        }
        else if (characterController)
        {
            characterController.enabled = true;
            // If you have your own movement system, feed it this velocity.
        }

        StartCoroutine(ReenableScriptsAfter(reenableDelay));
        onReleased?.Invoke();
    }

    public void EndGrabKill()
    {
        if (!_isGrabbed) return;
        _isGrabbed = false;

        if (_holdRoutine != null) StopCoroutine(_holdRoutine);

//#if CINEMACHINE
        if (grabCam) grabCam.Priority = 0;
        if (playerCam) playerCam.Priority = 50;
//#endif

        onKilled?.Invoke();

        // Keep scripts disabled; your death system should take over here.
        // Example: PlayerHealth.Kill(), fade out, reload checkpoint, etc.
    }

    IEnumerator ReenableScriptsAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (characterController) characterController.enabled = true;
        if (rb) rb.isKinematic = false;

        foreach (var s in scriptsToDisable)
            if (s) s.enabled = true;

    }
}
