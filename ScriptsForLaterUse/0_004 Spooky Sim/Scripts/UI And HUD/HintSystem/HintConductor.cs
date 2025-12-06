using UnityEngine;

public class HintConductor : MonoBehaviour
{
    WorldHintService _hintService;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<WorldHintService>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ShowHintEquimpent()
    {
        FindFirstObjectByType<WorldHintService>().ShowInFront(Camera.main, "Press 'E' to pick up equipment.", 1, 5);
    }

    public void ShowHintInteract()
    {
        FindFirstObjectByType<WorldHintService>().ShowInFront(Camera.main, "Press 'E' to interact.", 1, 5);
    }
}
