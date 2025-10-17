using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StatePatrol : IState
{

    float direction = -1;
    public float rayDistance = 3.0f;
    Vector3 destination;
    Timer timer;
    float delay = 0.5f;
    private Vector3 velocity;
    private float speed = 6;


    public override void EnterState(StateMachine stateMachine)
    {
        stateMachine.target = null;
        StateCondtion(stateMachine);
        Debug.Log("New Destination: " + destination);
        destination = stateMachine.pathFinding.GetDestination(Vector3.right * direction);
        Debug.Log("New Destination: " + destination);
        timer = new Timer(delay);
        velocity = Vector3.zero;
    }

    public override void UpdateState(StateMachine stateMachine)
    {
        if (stateMachine.target == null)
        {
            MoveAlongPatrol(stateMachine);
            LookForPlayer(stateMachine);

        }
        else
        {
            stateMachine.TransitionToNextState();
        }
    }
    private void SetUpState() 
    { 
    
    }

    public void StateCondtion(StateMachine stateMachine)
    {
        LookForPlayer(stateMachine);
        if(stateMachine.target != null)
        {
            stateMachine.TransitionToNextState();
        }
    }

    private void LookForPlayer(StateMachine stateMachine)
    {

    }
    private void MoveAlongPatrol(StateMachine stateMachine)
    {





        Destination(stateMachine);
        Direction(stateMachine);

        if (ShouldJump(stateMachine)) { Jump(stateMachine);}
        else { CalculateMovement(stateMachine, direction); }

        if(stateMachine.rb.linearVelocity.x != 0 && stateMachine.rb.linearVelocity.y ==0) stateMachine.animator.SetBool("walk", true);
        else stateMachine.animator.SetBool("walk", false);
    }

    private void CalculateMovement(StateMachine stateMachine, float direction)
    {
        
        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = speed * direction;
        //We can reduce are control using Lerp() this smooths changes to are direction and speed
        targetSpeed = Mathf.Lerp(stateMachine.rb.linearVelocity.x, targetSpeed, 1);

        float accelRate;

        accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? 1.5f : 2.5f;

        //Calculate difference between current velocity and desired velocity
        float speedDif = targetSpeed - stateMachine.rb.linearVelocity.x;
        //Calculate force along x-axis to apply to thr player


        float movement = speedDif * accelRate;


        //Convert this to a vector and apply to rigidbody
        Debug.Log(movement);
        stateMachine.rb.AddForce(movement * Vector3.right, ForceMode.Force);

        //velocity.x = speed * direction;
    }

    private void GravityCheck(StateMachine stateMachine)
    {
        if (difference(stateMachine.gameObject.transform.position.x, destination.x) < 1.2f && destination.y < stateMachine.gameObject.transform.position.y+0.8f)
        {
            stateMachine.rb.AddForce(Vector3.down, ForceMode.Impulse);
        }
    }

    private bool ShouldJump(StateMachine stateMachine) 
    {
        if (difference(stateMachine.gameObject.transform.position.x, destination.x) < 1.2f && destination.y > stateMachine.gameObject.transform.position.y - 0.4f)
        {
            return true;
        }

        return false;
    }

    private void Destination(StateMachine stateMachine)
    {
        if (difference(stateMachine.gameObject.transform.position.x, destination.x) < 0.4f && 
            difference(stateMachine.gameObject.transform.position.y, destination.y) < 0.4f)
        {
            destination = stateMachine.pathFinding.GetDestination(Vector3.right * direction);
            Debug.Log("New Destination: " + destination);
        }

    }

    private void Direction(StateMachine stateMachine)
    {

        float dist1 = Vector3.Distance(destination, stateMachine.gameObject.transform.position + Vector3.right * direction);
        float dist2 = Vector3.Distance(destination, stateMachine.gameObject.transform.position + (Vector3.right * direction * -1));


        if (dist1 > dist2)
        {
            direction *= -1;

            stateMachine.gameObject.transform.Rotate(0, 180, 0);
        }

    }

    private float difference(float x1, float x2)
    {
        if (x1 > x2)
        {
            return x1 - x2;
        }
        else 
        { 
            return x2 - x1; 
        }
    }

    private void Jump(StateMachine stateMachine)
    {

        Debug.Log("Jump");
        if (timer.ClockTick())
        {
            stateMachine.rb.AddForce(new Vector3(0, 1, 0) * 10, ForceMode.Impulse);
            timer.RestartTimer();

            stateMachine.animator.SetBool("walk", false);
            stateMachine.animator.SetTrigger("jump");
        }
    }

    private void FindPlayer(StateMachine stateMachine)
    {

    }
    bool IsWallInFront(Vector3 direction, StateMachine stateMachine)
    {


       /*
                float thickness = 1f; //<-- Desired thickness here.
                Vector3 origin = stateMachine.gameObject.transform.position + new Vector3(0, 1.6f, 0);
                RaycastHit hit;



                if (Physics.SphereCast(origin, thickness, direction, out hit))
                {
                    // A wall is detected
                    if(hit.collider.gameObject.layer == 6)
                    return true;
                }
        
                */
                /*
               // Get all colliders that overlap with the trigger collider
               Collider[] overlappingColliders = Physics.OverlapBox(
                   stateMachine.WallDectetor.bounds.center, stateMachine.WallDectetor.bounds.extents, 
                   stateMachine.WallDectetor.transform.rotation, 6);

               foreach (Collider collider in overlappingColliders)
               {
                       Debug.Log("Overlap detected with: " + collider.name);
                   return true;

               }
               */

        /*
        // Shoot a ray from the monster's position in the specified direction
        Vector3 rayPosition = new Vector3(stateMachine.gameObject.transform.position.x, 
            stateMachine.gameObject.transform.position.y + 0.5f, stateMachine.gameObject.transform.position.z-0.3f);
        Ray ray = new Ray(rayPosition, direction);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);
        // Check if the ray hits a wall within the specified distance
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.Log("True");
            // A wall is detected
            return true;
        }*/


        // No wall detected
        return false;
    }




}
