using UnityEngine;

public abstract class Anomaly : MonoBehaviour
{
    public string anomalyName;
    public bool lethal = false;

    // Called when player triggers the anomaly
    public abstract void Activate(GameObject player);
}
