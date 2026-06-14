using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerAvatar : NetworkBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float turnSpeed = 360f;

    void Update()
    {
        if (!IsOwner) return; // local control only

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Simple movement (XZ plane)
        var dir = new Vector3(h, 0, v).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}
