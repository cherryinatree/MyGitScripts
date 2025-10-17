using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forward : MonoBehaviour
{
    public Directrion.JumpType jumpType = Directrion.JumpType.Left;

    public float speed =1;

    public GeneralTrigger trigger;

    // Start is called before the first frame update
    void Start()
    {
        if (trigger != null)
        {
            if (trigger.TriggerActivated)
            {

                GetComponent<Rigidbody>().velocity = Directrion.JumpDirection[(int)jumpType] * speed;
            }
        }
        else
        {

            GetComponent<Rigidbody>().velocity = Directrion.JumpDirection[(int)jumpType] * speed;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (trigger != null)
        {
            Debug.Log(trigger.TriggerActivated);
            if (trigger.TriggerActivated)
            {

                GetComponent<Rigidbody>().velocity = Directrion.JumpDirection[(int)jumpType] * speed;
            }
        }
        else
        {

            GetComponent<Rigidbody>().velocity = Directrion.JumpDirection[(int)jumpType] * speed;
        }
    }
}
