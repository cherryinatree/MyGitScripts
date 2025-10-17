using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonActive : MonoBehaviour
{

    public ButtonPad pad;
    public GameObject[] ActiveObjects;
    public bool invertBool = false;

    private bool isActive = true;
    public bool isActiveAtStart = true;
    // Start is called before the first frame update
    void Start()
    {
        if (!isActiveAtStart)
        {
            isActive = false;
            for (int i = 0; i < ActiveObjects.Length; i++)
            {
                ActiveObjects[i].SetActive(isActive);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(pad != null)
        {
            if(ActiveObjects != null)
            {
                if(invertBool)
                {
                    if (isActive == pad.isPressed)
                    {
                        isActive = !isActive;
                        for (int i = 0; i < ActiveObjects.Length; i++)
                        {
                            if (ActiveObjects[i] != null)
                            {
                                ActiveObjects[i].SetActive(isActive);
                            }
                        }
                    }
                }
                else
                {

                    if (isActive != pad.isPressed)
                    {
                        isActive = !isActive;
                        for (int i = 0; i < ActiveObjects.Length; i++)
                        {

                            if (ActiveObjects[i] != null)
                            {
                                ActiveObjects[i].SetActive(isActive);
                            }
                        }
                    }
                }
            }
        }
    }
}
