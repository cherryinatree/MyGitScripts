using UnityEngine;

[CreateAssetMenu(menuName = "Cherry/Beams/Beam Ray Definition", fileName = "Ray_")]
public class BeamRayDefinition : ScriptableObject
{
    public string rayId = "collector";
    public Color color = Color.white;

    [Header("Combat")]
    [Tooltip("Damage per second. 0 means this ray never damages enemies.")]
    public float damagePerSecond = 0f;

    [Header("Harvest")]
    public bool canHarvest = true;

    [Header("Optics")]
    public bool canBeReflected = true;
}