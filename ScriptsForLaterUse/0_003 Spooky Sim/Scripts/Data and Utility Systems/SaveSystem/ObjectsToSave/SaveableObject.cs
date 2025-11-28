// SaveableObject.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SaveableObject : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string objectId;    // auto-generated if empty
    [Tooltip("Resources path to the prefab (no .prefab). E.g. \"Placeables/Shelf_A\"")]
    [SerializeField] private string prefabPath;

    [Header("Auto Dirty (optional)")]
    [SerializeField] private bool autoDirty = true;
    [SerializeField] private float checkInterval = 0.2f;

    private Vector3 _lastPos;
    private Quaternion _lastRot;
    private float _nextCheck;

    public string ObjectId => objectId;
    public string PrefabPath => prefabPath;

    private void Reset()
    {
        if (string.IsNullOrWhiteSpace(objectId))
            objectId = Guid.NewGuid().ToString("N");
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(objectId))
            objectId = Guid.NewGuid().ToString("N");
    }

    private void OnEnable()
    {
        WorldObjectSaveMaster.Instance?.Register(this);
        _lastPos = transform.position;
        _lastRot = transform.rotation;
    }

    private void OnDisable()
    {
        WorldObjectSaveMaster.Instance?.Unregister(this);
    }

    private void Update()
    {
        if (!autoDirty) return;
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + checkInterval;

        if (transform.position != _lastPos || transform.rotation != _lastRot)
        {
            _lastPos = transform.position;
            _lastRot = transform.rotation;
            MarkDirty();
        }
    }

    /// <summary>Call this manually after your placement/rotation ops if you keep autoDirty off.</summary>
    public void MarkDirty()
    {
        WorldObjectSaveMaster.Instance?.NotifyObjectChanged(this);
    }

    public PlacedObjectData Capture()
    {
        return new PlacedObjectData
        {
            id = objectId,
            prefabPath = prefabPath,
            position = transform.position,
            rotation = transform.rotation,
            sceneBelongingTo = gameObject.scene.name
        };
    }

    /// <summary>Used during load to apply saved transform & ensure ids/paths match.</summary>
    public void Apply(PlacedObjectData data)
    {
        objectId = data.id;
        if (!string.IsNullOrWhiteSpace(data.prefabPath))
            prefabPath = data.prefabPath;

        transform.SetPositionAndRotation(data.position, data.rotation);
        _lastPos = data.position;
        _lastRot = data.rotation;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate New GUID")]
    private void GenerateGuid()
    {
        objectId = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
