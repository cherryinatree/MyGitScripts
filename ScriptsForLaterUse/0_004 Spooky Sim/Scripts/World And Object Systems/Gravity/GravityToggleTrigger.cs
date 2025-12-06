using UnityEngine;

public class GravityToggleTrigger : MonoBehaviour
{
    

    public void TurnOnGravity(GameObject other)
    {
       other.GetComponent<Rigidbody>().useGravity = true;
    }


    public void TurnOffGravity(GameObject other)
    {
        Debug.Log("Gravity Off");
        Debug.Log(other.name);
        other.GetComponent<Rigidbody>().useGravity = false;
    }
}
