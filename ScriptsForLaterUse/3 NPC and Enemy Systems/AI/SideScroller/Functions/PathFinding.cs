using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PathFinding : MonoBehaviour
{

    private GameObject[] walkingSquares;
    private Vector2[] gridPoints;
    private GameObject currentSquare;
    private GameObject nextSquare;

    public float jumpDistance = 5f;


    // Start is called before the first frame update
    void Start()
    {
        walkingSquares = GameObject.FindGameObjectsWithTag("Walkable");
        gridPoints = new Vector2[walkingSquares.Length];
        for (int i = 0; i < walkingSquares.Length; i++)
        {
            gridPoints[i] = walkingSquares[i].transform.position;
        }
    }


    public Vector3 GetDestination(Vector3 direction)
    {
        FindMySquare();
        nextSquare = currentSquare;
        CalculateDestination(direction);
        CheckForEdge(direction);

        Vector3 destination = new Vector3(nextSquare.transform.position.x, nextSquare.transform.position.y + 1, nextSquare.transform.position.z);
        return destination;
    }

    private Vector3 CalculateDestination(Vector3 direction)
    {

        bool foundCloser = false;
        float distance = 0.5f;


        for (int i = 0; i < walkingSquares.Length; i++)
        {
            if(walkingSquares[i].transform.position.y == nextSquare.transform.position.y)
            {
                if (distance > Vector3.Distance(nextSquare.transform.position + direction, walkingSquares[i].transform.position))
                {
                    if (nextSquare != walkingSquares[i]) 
                    { 
                        nextSquare = walkingSquares[i];
                        foundCloser = true;
                    }
                }
            }

        }

        if (foundCloser)
        {
            CalculateDestination(direction);
        }
        return direction;
    }

    private void CheckForEdge(Vector3 direction)
    {
        if(nextSquare == currentSquare)
        {
            bool Jump = LookForJump(Vector3.right);
            if (Jump) return;
            bool fall = LookForFall(Vector3.right);
            if (fall) return;
            CalculateDestination(-direction);
        }
    }

    private bool LookForJump(Vector3 direction)
    {

        bool jump = false;
        float distance = 0.5f;


        for (int i = 0; i < walkingSquares.Length; i++)
        {
            if (walkingSquares[i].transform.position.y > currentSquare.transform.position.y)
            {
                float xSeperation = Vector3.Distance(new Vector3((currentSquare.transform.position.x + direction.x),0,0), 
                    new Vector3(walkingSquares[i].transform.position.x,0,0));
                if (distance > xSeperation)
                {
                    if(5 > Vector3.Distance(currentSquare.transform.position + direction, walkingSquares[i].transform.position))
                    {
                        if (nextSquare != walkingSquares[i])
                        {
                            nextSquare = walkingSquares[i];
                            jump = true;
                        }
                    }
                }
            }

        }

        return jump;
    }

    private bool LookForFall(Vector3 direction)
    {


        bool fall = false;
        float distance = 0.5f;


        for (int i = 0; i < walkingSquares.Length; i++)
        {
            if (walkingSquares[i].transform.position.y < currentSquare.transform.position.y)
            {
                float xSeperation = Vector3.Distance(new Vector3((currentSquare.transform.position.x + direction.x), 0, 0),
                    new Vector3(walkingSquares[i].transform.position.x, 0, 0));
                if (distance > xSeperation)
                {
                    if (nextSquare != walkingSquares[i])
                    {
                        nextSquare = walkingSquares[i];
                        fall = true;
                    }
                }
            }

        }

        return fall;
    }

    private bool ChangeDirection(Vector3 direction)
    {
        bool change = false;
        //float distance = 0.5f;




        return change;
    }

    private void FindMySquare()
    {
        bool foundCloser = false;

        if(currentSquare == null)
        {
            if (walkingSquares == null) { walkingSquares = GameObject.FindGameObjectsWithTag("Walkable"); }
            currentSquare = walkingSquares[0];
        }

        float distance = Vector3.Distance(transform.position + Vector3.down, currentSquare.transform.position);



        for (int i = 0; i < walkingSquares.Length; i++)
        {
            if (distance > Vector3.Distance(transform.position + Vector3.down, walkingSquares[i].transform.position))
            {
                if (currentSquare != walkingSquares[i])
                {
                    distance = Vector3.Distance(transform.position + Vector3.down, walkingSquares[i].transform.position);
                    currentSquare = walkingSquares[i];
                    foundCloser = true;
                }
            }
        }

        if(foundCloser)
        {
            FindMySquare();
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
