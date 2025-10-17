using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightChange : MonoBehaviour
{
    public ButtonPad pad;
    public EffectsTimer Et;

    public bool useTimer = false;
    public bool inverseBool = false;
    public bool useThePad = true;

    public Color color0 = Color.green;
    public Color color1 = Color.red;


    private MeshRenderer renderer;


    private void Start()
    {
        renderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (useTimer)
        {
                InverseTheBool(Et.triggerActivated);
            
        }
        if (useThePad)
        {
            InverseTheBool(pad.isPressed);
        }
    }

    private void InverseTheBool(bool origionalBool)
    {
        if (inverseBool)
        {
            origionalBool = !origionalBool;
        }

        if (origionalBool)
        {

            renderer.material.color = color0;
        }
        else
        {
            renderer.material.color = color1;
        }
    }
}
