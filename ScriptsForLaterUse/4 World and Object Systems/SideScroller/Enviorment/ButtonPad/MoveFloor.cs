using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveFloor : MonoBehaviour
{
    private float sizeX;
    private Vector3 origionalPosition;
    private Vector3 movePosition;
    public float speed = 2;
    public bool customDistance = false;
    public float distance = 10;

    public bool useRigidbody = false;

    public enum DelayType { DelayStart = 0, DelayEnd = 1, DelayBoth = 2, DelayNone = 3 }
    public DelayType delayType = DelayType.DelayNone;

    public float delay = 1;
    private Rigidbody rb;

    public Directrion.JumpType direction;

    public ButtonPad pad;
    public bool isPushPad = false;

    private Timer timer;

    // Start is called before the first frame update
    void Start()
    {
        timer = new Timer(delay);
        
        sizeX = transform.localScale.x;
        origionalPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        if (customDistance)
        {
            sizeX = distance;
        }
        movePosition = origionalPosition + (Directrion.JumpDirection[(int)direction] * sizeX);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPushPad)
        {
            if (pad != null)
            {
                if (pad.isPressed)
                {
                    if (transform.position != movePosition)
                    {
                        if (delayType == DelayType.DelayStart || delayType == DelayType.DelayBoth)
                        {
                            if (timer.ClockTick())
                            {
                                movement(movePosition);
                            }
                        }
                        else
                        {
                            movement(movePosition);
                        }

                    }
                    else
                    {

                        timer.RestartTimer();
                    }
                }
                else
                {

                    if (transform.position != origionalPosition)
                    {
                        if (delayType == DelayType.DelayEnd || delayType == DelayType.DelayBoth)
                        {
                            if (timer.ClockTick())
                            {
                                movement(origionalPosition);
                            }
                        }
                        else
                        {
                            movement(origionalPosition);
                        }
                    }
                    else
                    {

                        timer.RestartTimer();
                    }
                }
            }
        }
        else
        {

            if (transform.position != movePosition)
            {
                if (delayType == DelayType.DelayStart || delayType == DelayType.DelayBoth)
                {
                    if (timer.ClockTick())
                    {
                        movement(movePosition);
                    }
                }
                else
                {

                    movement(movePosition);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void movement(Vector3 position)
    {

        if (!useRigidbody)
        {
            transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
        }
        else
        {

            rb.MovePosition(Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime));
        }
    }
}
