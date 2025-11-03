using UnityEngine;

public class PanelActivate : MonoBehaviour
{
    public GameObject panelToActivate;

    public void ActivatePanel()
    {
        if (panelToActivate != null)
        {
            panelToActivate.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Panel to activate is not assigned.");
        }
    }

    public void DeactivatePanel()     
    {
        Debug.Log("Deactivating panel...");
        if (panelToActivate != null)
        {
            panelToActivate.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Panel to deactivate is not assigned.");
        }
    }
}
