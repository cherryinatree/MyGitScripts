using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitySpawner : MonoBehaviour
{
    public GameObject[] Ability;

    public GameObject Spawner;
    public float speed = 4;
    private float multiplyer= 2;
    public AudioSource soundEffect;

    public int currentAbility = 0;
    public float GroundDistance = 1f;

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.E) && GameSingleton.Instance.save.Charges>0)
        {
            GameObject clone = Instantiate(Ability[currentAbility], Spawner.transform.position, Quaternion.identity);
            Debug.Log(RayDown(Spawner.transform.forward) * speed);
            clone.GetComponent<Rigidbody>().velocity = (RayDown(Spawner.transform.forward) * speed);
            clone.layer = 0;
            ResourceManager.ChargeModify(-1);
            soundEffect.Play();
        }
    }

    public void Firing()
    {
        if (GameSingleton.Instance.save.Charges > 0)
        {
            GameObject clone = Instantiate(Ability[currentAbility], Spawner.transform.position, Quaternion.identity);
            clone.GetComponent<Rigidbody>().velocity = (RayDown(Spawner.transform.forward) * speed);
            clone.layer = 0;
            ResourceManager.ChargeModify(-1);
            soundEffect.Play();
        }
    }
    private Vector3 RayDown(Vector3 forward)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit))
        {
            //Debug.Log(hit.distance);
            if (hit.distance < GroundDistance)
            {
                if(hit.transform.tag == "Ground"|| hit.transform.tag == "Floor")
                {
                    float newY = hit.transform.rotation.z;
                    if (hit.transform.rotation.z > 0)
                    {
                        if (forward.x > 0)
                        {
                            newY *= multiplyer;
                        }
                        else
                        {
                            newY *= -multiplyer;
                        }
                    }
                    if (hit.transform.rotation.z < -0)
                    {
                        if (forward.x > 0)
                        {
                            newY *= multiplyer;
                        }
                        else
                        {
                            newY *= -multiplyer;
                        }
                    }
                    return forward = new Vector3(forward.x, newY, forward.z);
                }
            }
            else
            {
                return forward;
            }
        }
        return forward;

    }
}
