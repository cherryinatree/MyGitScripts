using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTowards : MonoBehaviour
{

    public Vector3 Destination;
    public float speed = 4;

       // Base speed of the spell
    public float delay = 0.5f;    // Amplitude of the wave
    private float timeOffset = 0f;  // Offset to synchronize the wave across instances


    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move()
    {
        if(Destination != null)
        {
            timeOffset += Time.deltaTime;



            // Calculate the wave-based speed using the sine or cosine function
            //float waveSpeed = Sigmoid((amplitude * frequency * (timeOffset))*10);

            //Debug.Log((waveSpeed * Time.deltaTime) * 10);

            // Move the spell object in the desired direction
            if (timeOffset > delay)
            {
                transform.position = Vector3.MoveTowards(transform.position, Destination, (speed * Time.deltaTime));
            }
            if(transform.position == Destination)
            {
                Destroy(gameObject);
            }
        }
    }

    float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-(x)-8));
    }
}
