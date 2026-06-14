using UnityEngine;

public enum BossWaypointMoveMode
{
    Run,
    Fly
}

[AddComponentMenu("Cherry/Bosses/Boss Waypoint")]
public class BossWaypoint : MonoBehaviour
{
    public BossWaypointMoveMode moveMode = BossWaypointMoveMode.Run;
    [Min(0.05f)] public float arrivalRadius = 0.75f;
}