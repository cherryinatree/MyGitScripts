using UnityEngine;


public class HeadlightsController : MonoBehaviour
{
    public Light[] headlights;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            foreach (Light light in headlights)
                light.enabled = !light.enabled;
        }
    }
}