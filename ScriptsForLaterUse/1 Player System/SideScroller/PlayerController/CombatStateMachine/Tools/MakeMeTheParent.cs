using UnityEngine;

public class MakeMeTheParent : MonoBehaviour
{

    public Transform newParent;

    private bool hasParent = false; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(!hasParent)
        {
            transform.parent = null;
            newParent.parent = transform;
            hasParent = true;
        }
    }
}
