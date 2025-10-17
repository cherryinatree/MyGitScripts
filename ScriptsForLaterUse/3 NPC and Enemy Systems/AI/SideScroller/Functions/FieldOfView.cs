
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [HideInInspector]
    public float viewRadius;
    [Range(0, 360)]
    [Tooltip("This is the angle of the field of view for the AI.")]
    public float viewAngle = 360;

    [Tooltip("You need to assign the player layer here because remember - the player is an enemy of our enemy :P")]
    public LayerMask targetMask;
    [Tooltip("Here all the building layers should be checked.")]
    public LayerMask obstacleMask;

    [HideInInspector]
    public bool canSee;

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();
    private List<GameObject> parents = new List<GameObject>();

    void Start() //This method is called once the game starts.
    {
        StartCoroutine("FindTargetsWithDelay", .2f); //And so is the coroutine "FindTargetsWithDelay".
    }

    private void Update()
    {
        if (visibleTargets.Count > 0) //A quick check if the targets are less than zero
        {                            //Then our AI knows he sees nothing 
            canSee = true;
        }
        else canSee = false;
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, 5, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                }
            }
        }
    }


    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    private void OnDrawGizmos()
    {
        Debug.Log("Drawing Gizmos");
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 5);
        
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
}
