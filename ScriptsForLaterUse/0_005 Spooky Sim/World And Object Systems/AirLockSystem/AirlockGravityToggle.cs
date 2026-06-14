using Cherry.Airlocks;
using UnityEngine;

public class AirlockGravityToggle : MonoBehaviour
{
    
    public AirlockController airlockController;
    public PressureVolume airlockPressureVolume;
    public BoxCollider airlockTriggerZone;
    public Vector3 nudgeDirection = Vector3.up;
    public float nudgeForce = 0.1f;

    public void DecideIfGravityOff(GameObject other)
    {
        if(other.GetComponent<Rigidbody>() == null) return;
       if (airlockPressureVolume.State == PressureVolume.PressureState.Vacuum || airlockPressureVolume.State == PressureVolume.PressureState.Depressurizing)
        {
            TurnOffGravity(other);
        }
        else
        {
            TurnOnGravity(other);
        }
    }

    public void TurnOnGravity(GameObject other)
    {
        other.GetComponent<Rigidbody>().useGravity = true;
        
    }

    public void TurnOffGravity(GameObject other)
    {
        if (other.GetComponent<Rigidbody>().useGravity == true)
        {
            other.GetComponent<Rigidbody>().AddForce(nudgeDirection * nudgeForce, ForceMode.Impulse);
        }
            other.GetComponent<Rigidbody>().useGravity = false;
    }
}
