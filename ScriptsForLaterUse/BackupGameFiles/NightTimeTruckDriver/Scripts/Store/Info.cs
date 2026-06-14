using UnityEngine;
using TMPro;

public class Info : MonoBehaviour
{
    public TMP_Text infoText; // Reference to the TextMeshPro text component


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        infoText.text = "Money: " + SaveSingleton.Instance.truckStats.money.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
