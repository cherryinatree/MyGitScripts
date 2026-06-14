using UnityEngine;

[AddComponentMenu("AI/Customers/Customer Loiter Point")]
public class CustomerLoiterPoint : MonoBehaviour
{
    [Tooltip("Where the customer should generally stand to inspect this. If null, uses this transform.")]
    public Transform standPoint;

    [Tooltip("What the customer should look at while inspecting. If null, looks at this transform.")]
    public Transform lookAt;

    [Range(0.1f, 10f)] public float weight = 1f;

    [Header("Viewing Area")]
    [Tooltip("Customers can stand anywhere within this radius of StandPosition.")]
    public float standRadius = 1.2f;

    [Tooltip("How far apart customers should try to stay while choosing a spot.")]
    public float minSeparation = 0.8f;

    [Tooltip("If there are already this many customers near the point, prefer another point.")]
    public int softMaxViewers = 2;

    [Header("Optional per-point timing override (leave 0 to use state defaults)")]
    public float minLookSecondsOverride = 0f;
    public float maxLookSecondsOverride = 0f;

    public Vector3 StandPosition => standPoint ? standPoint.position : transform.position;
    public Vector3 LookPosition => lookAt ? lookAt.position : transform.position;
}
