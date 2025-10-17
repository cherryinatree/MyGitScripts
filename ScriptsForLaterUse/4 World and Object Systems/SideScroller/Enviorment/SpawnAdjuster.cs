using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnAdjuster : MonoBehaviour
{
    public Transform[] patrolPoints;

    public bool inverseBool = false;
    public EffectsTimer Et;
    public Spawner spawner;
    public ButtonPad pad;

    private bool newBool;

    public void AdjustMe(GameObject spawned)
    {
        if(patrolPoints != null)
        {
            if (patrolPoints.Length > 0)
            {
                if (spawned.GetComponent<Sentry>())
                {
                    Sentry patrol = spawned.GetComponent<Sentry>();
                    patrol.points = new Transform[patrolPoints.Length];
                    for (int i = 0; i < patrolPoints.Length; i++)
                    {
                        patrol.points[i] = patrolPoints[i];
                    }
                }
            }
        }
    }

    private void Update()
    {
        if(Et != null)
        {
            if(spawner != null)
            {
                if (inverseBool)
                {
                    newBool = !Et.triggerActivated;
                }
                else
                {

                    newBool = Et.triggerActivated;
                }

                spawner.isOn = newBool;
            }
        }
        else if (pad != null)
        {

            if (spawner != null)
            {
                if (inverseBool)
                {
                    newBool = !pad.isPressed;
                }
                else
                {

                    newBool = pad.isPressed;
                }

                spawner.isOn = newBool;
            }
        }
    }
}
