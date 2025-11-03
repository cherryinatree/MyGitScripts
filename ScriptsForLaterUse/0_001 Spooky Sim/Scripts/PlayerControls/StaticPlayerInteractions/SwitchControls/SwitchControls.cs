using UnityEngine;

public class SwitchControls : MonoBehaviour
{

    public MonoBehaviour Flop0;
    public MonoBehaviour Flop1;

    private bool isFlop0Active = true;

    private void Start()
    {
        Flop0.enabled = true;
        Flop1.enabled = false;
    }

    public void FlopScripts()
    {
        if (isFlop0Active)
        {
            Flop0.enabled = false;
            Flop1.enabled = true;
            isFlop0Active = false;
        }
        else
        {
            Flop0.enabled = true;
            Flop1.enabled = false;
            isFlop0Active = true;
        }
    }
}
