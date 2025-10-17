using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class Ai_Eyes : MonoBehaviour
{

    public enum EyeSight
    {
        None,
        Ground,
        Default,
        Player
    }

    private Vector2[,] viewPosition;
    private EyeSight[,] viewObject;

    public float rayDistance = 10f; // Distance of the raycast
    public LayerMask layerMask; // Layer mask to filter the raycast
    float xDistance = 5;
    float yDistance = 10.2f;
    [HideInInspector]
    public GameObject lastPlayerSeen;

    [HideInInspector]
    public bool shouldJumpLeft = false;
    [HideInInspector]
    public bool shouldJumpRight = false;

    public bool IsPlayerInSight()
    {
        for (int i = 0; i < 21; i++)
        {
            if (viewObject[0, i] == EyeSight.Player || viewObject[1, i] == EyeSight.Player)
            {
                return true;
            }
        }
        return false;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        viewPosition = new Vector2[2, 21];
        viewObject = new EyeSight[2, 21];
        lastPlayerSeen = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        LookAroundAllAtOnce();
        UpdateJump();
    }

    private void UpdateJump()
    {
        if (viewPosition[0, 9].y > viewPosition[0, 11].y + 0.6f)
        {
            shouldJumpRight = true;
            Debug.Log("should jump left");
        }
        else
        {
            shouldJumpRight = false;
        }
        if (viewPosition[0, 11].y > viewPosition[0, 9].y+0.6f)
        {
            shouldJumpLeft = true;
            Debug.Log("should jump right");
        }
        else
        {
            shouldJumpLeft = false;
        }
    }

    private void LookAroundAllAtOnce()
    {

        // Define the direction of the raycast (forward direction of the AI character)
        // Vector3 forward = transform.TransformDirection(Vector3.forward) * rayDistance;
        for (int i = 0; i < 20; i++)
        {
            if (xDistance > -10)
            {
                xDistance--;
            }
            else
            {
                xDistance = 10;
            }

            Vector3 position = new Vector3(transform.position.x + xDistance, transform.position.y + yDistance,
                transform.position.z);
            Vector3 position2 = new Vector3(transform.position.x + xDistance, transform.position.y + 0.2f,
                transform.position.z);

            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, rayDistance, layerMask);
            RaycastHit2D hit2 = Physics2D.Raycast(position2, Vector2.down, rayDistance, layerMask);

            // Perform the raycast
            if (hit.collider != null)
            {
                if (xDistance == -1 || xDistance == 1)
                {
                    Debug.DrawRay(position, Vector3.down * rayDistance, Color.white);
                }
                else
                {

                    Debug.DrawRay(position, Vector3.down * rayDistance, Color.green);
                }
                UpdateDictionary(hit.collider.gameObject, hit.point, 0, (int)xDistance + 10);
            }
            else
            {
                if(xDistance == -1 || xDistance == 1)
                {
                    Debug.DrawRay(position, Vector3.down * rayDistance, Color.blue);
                }
                else
                {

                    Debug.DrawRay(position, Vector3.down * rayDistance, Color.red);
                }
                UpdateDictionary(0, (int)xDistance + 10);
            }

            if (hit2.collider != null)
            {
                if (xDistance == -1 || xDistance == 1)
                {
                    Debug.DrawRay(position2, Vector3.down * rayDistance, Color.white);
                }
                else
                {

                    Debug.DrawRay(position2, Vector3.down * rayDistance, Color.green);
                }
                UpdateDictionary(hit2.collider.gameObject, hit.point, 1, (int)xDistance + 10);
            }
            else
            {
                if (xDistance == -1 || xDistance == 1)
                {
                    Debug.DrawRay(position2, Vector3.down * rayDistance, Color.blue);
                }
                else
                {

                    Debug.DrawRay(position2, Vector3.down * rayDistance, Color.red);
                }
                UpdateDictionary(1, (int)xDistance + 9);
            }
        }
    }

    private void LookAround()
    {

        // Define the direction of the raycast (forward direction of the AI character)
       // Vector3 forward = transform.TransformDirection(Vector3.forward) * rayDistance;

        if(xDistance > -10)
        {
            xDistance--;
        }else
        {
            xDistance = 10;
        }

        Vector3 position = new Vector3(transform.position.x + xDistance, transform.position.y + yDistance,
            transform.position.z);
        Vector3 position2 = new Vector3(transform.position.x + xDistance, transform.position.y + 0.2f,
            transform.position.z);

        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, rayDistance, layerMask);
        RaycastHit2D hit2 = Physics2D.Raycast(position2, Vector2.down, rayDistance, layerMask);

        // Perform the raycast
        if (hit.collider != null)
        {
            Debug.DrawRay(position, Vector3.down * rayDistance, Color.green);
            UpdateDictionary(hit.collider.gameObject, hit.point, 0, (int)xDistance + 10);
        }
        else
        {
            Debug.DrawRay(position, Vector3.down * rayDistance, Color.red);
            UpdateDictionary(0, (int)xDistance + 10);
        }

        if (hit2.collider != null)
        {
            Debug.DrawRay(position2, Vector3.down * rayDistance, Color.green);
            UpdateDictionary(hit2.collider.gameObject, hit.point, 1, (int)xDistance + 10);
        }
        else
        {
            Debug.DrawRay(position2, Vector3.down * rayDistance, Color.red);
            UpdateDictionary(1, (int)xDistance + 9);
        }
    }

    private void UpdateDictionary(GameObject hit, Vector2 hitPoint, int verticle, int horizontal)
    {
        if (hit.layer == 8)
        {
            viewPosition[verticle, horizontal] = hitPoint;
            viewObject[verticle, horizontal] = EyeSight.Ground;
        }
        else if (hit.layer == 0)
        {
            viewPosition[verticle, horizontal] = hitPoint;
            viewObject[verticle, horizontal] = EyeSight.Default;
        }
        else if (hit.layer==9)
        {
            viewPosition[verticle, horizontal] = hitPoint;
            viewObject[verticle, horizontal] = EyeSight.Player;
            lastPlayerSeen = hit;
        }
        else
        {

            viewPosition[verticle, horizontal] = hitPoint;
            viewObject[verticle, horizontal] = EyeSight.None;
        }
    }
    private void UpdateDictionary(int verticle, int horizontal)
    {

        viewPosition[verticle, horizontal] = Vector3.zero;
        viewObject[verticle, horizontal] = EyeSight.None;
    }
}
