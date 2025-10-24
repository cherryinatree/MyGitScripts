using UnityEngine;

public class DeativateOnEnable : MonoBehaviour
{
    private void OnEnable()
    {
        gameObject.SetActive(false);
    }
}
