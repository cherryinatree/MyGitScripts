using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            Debug.Log(1);
            //move = Vector3.right;
            gameObject.GetComponent<Rigidbody>().velocity = gameObject.transform.forward*5;
        }
        if (Input.GetKey(KeyCode.A))
        {
            //move = Vector3.left;
            gameObject.GetComponent<Rigidbody>().AddForce(-gameObject.transform.forward);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log(2);
            //verticalVel = 100;
            gameObject.GetComponent<Rigidbody>().AddForce(Vector3.up * 1000);
        }
    }
}
