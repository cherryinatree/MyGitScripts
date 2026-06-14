using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cherry.Portals
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Portals/Portal (Portal-style)")]
    public class Portal : MonoBehaviour
    {
        [Header("Link")]
        public Portal linkedPortal;

        [Header("Screen (the doorway plane)")]
        [SerializeField] private Transform screenPlane;     // plane that fills doorway opening
        [SerializeField] private MeshRenderer screenRenderer;

        [Header("Cameras")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Camera portalCamera;

        [Header("Rendering")]
        [SerializeField] private LayerMask portalCullingMask = ~0; // exclude PortalScreen layer here
        [SerializeField] private float nearClipOffset = 0.03f;

        [Header("Teleport")]
        [SerializeField] private float postTeleportOffset = 0.05f;
        [SerializeField] private float reTeleportCooldown = 0.10f; // prevents bounce

        private static readonly Quaternion Rot180 = Quaternion.Euler(0f, 180f, 0f);

        private readonly Dictionary<PortalTraveller, Vector3> _lastPivotPos = new();
        private readonly Dictionary<PortalTraveller, float> _cooldownUntil = new();

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;

            if (portalCamera != null)
            {
                portalCamera.enabled = false;
                portalCamera.cullingMask = portalCullingMask;
            }

            if (screenPlane == null) screenPlane = transform; // fallback, but better to assign the actual screen
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != playerCamera) return;
            if (linkedPortal == null || portalCamera == null) return;
            if (screenRenderer != null && !screenRenderer.isVisible) return;

            RenderView(cam);
        }

        private void RenderView(Camera srcCam)
        {
            // Place portalCamera so it matches srcCam�s pose through the portal
            Matrix4x4 m =
                linkedPortal.transform.localToWorldMatrix *
                Matrix4x4.Rotate(Rot180) *
                transform.worldToLocalMatrix *
                srcCam.transform.localToWorldMatrix;

            portalCamera.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);

            // Match lens
            portalCamera.fieldOfView = srcCam.fieldOfView;
            portalCamera.aspect = srcCam.aspect;
            portalCamera.nearClipPlane = srcCam.nearClipPlane;
            portalCamera.farClipPlane = srcCam.farClipPlane;

            // Oblique clip against the linked portal�s screen plane
            portalCamera.ResetProjectionMatrix();
            SetObliqueClipPlane(linkedPortal.screenPlane != null ? linkedPortal.screenPlane : linkedPortal.transform);

            portalCamera.Render();
        }

        private void SetObliqueClipPlane(Transform planeTransform)
        {
            Vector3 planePos = planeTransform.position;
            Vector3 planeNormal = planeTransform.forward;

            // Force plane normal to face the portal camera
            if (Vector3.Dot(planeNormal, portalCamera.transform.position - planePos) < 0f)
                planeNormal = -planeNormal;

            planePos += planeNormal * nearClipOffset;

            Matrix4x4 w2c = portalCamera.worldToCameraMatrix;
            Vector3 camSpacePos = w2c.MultiplyPoint(planePos);
            Vector3 camSpaceNormal = w2c.MultiplyVector(planeNormal).normalized;

            Vector4 clipPlane = new Vector4(
                camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z,
                -Vector3.Dot(camSpacePos, camSpaceNormal)
            );

            portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipPlane);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PortalTraveller t)) return;
            _lastPivotPos[t] = t.Pivot.position;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out PortalTraveller t)) return;
            _lastPivotPos.Remove(t);
        }

        private void OnTriggerStay(Collider other)
        {
            if (linkedPortal == null) return;
            if (!other.TryGetComponent(out PortalTraveller t)) return;

            // cooldown to prevent re-teleport bounce
            if (_cooldownUntil.TryGetValue(t, out float until) && Time.time < until) return;

            var pivot = t.Pivot;
            if (!_lastPivotPos.TryGetValue(t, out var prev)) prev = pivot.position;

            Vector3 curr = pivot.position;

            float d0 = Vector3.Dot(transform.forward, prev - screenPlane.position);
            float d1 = Vector3.Dot(transform.forward, curr - screenPlane.position);

            // If the segment crossed the plane, teleport
            if (Mathf.Sign(d0) != Mathf.Sign(d1))
            {
                Teleport(t);
                _cooldownUntil[t] = Time.time + reTeleportCooldown;

                // reset tracking after teleport
                _lastPivotPos[t] = t.Pivot.position;
                return;
            }

            _lastPivotPos[t] = curr;
        }

        private void Teleport(PortalTraveller t)
        {
            Transform tf = t.transform;

            Matrix4x4 m =
                linkedPortal.transform.localToWorldMatrix *
                Matrix4x4.Rotate(Rot180) *
                transform.worldToLocalMatrix *
                tf.localToWorldMatrix;

            Vector3 newPos = (Vector3)m.GetColumn(3) + linkedPortal.screenPlane.forward * postTeleportOffset;
            Quaternion newRot = m.rotation;

            // Preserve Rigidbody velocity if present
            Vector3 newVel = Vector3.zero;
            bool hasVel = false;

            if (t.rb != null)
            {
                Vector3 localVel = transform.InverseTransformDirection(t.rb.linearVelocity);
                localVel = Rot180 * localVel;
                newVel = linkedPortal.transform.TransformDirection(localVel);
                hasVel = true;
            }

            t.BeforeTeleport();
            tf.SetPositionAndRotation(newPos, newRot);
            t.AfterTeleport();

            if (t.rb != null && hasVel) t.rb.linearVelocity = newVel;
        }
    }
}
