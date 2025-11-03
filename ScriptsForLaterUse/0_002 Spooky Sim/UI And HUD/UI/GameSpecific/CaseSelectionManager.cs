using UnityEngine;

public class CaseSelectionManager : MonoBehaviour
{
    public string selectedCaseName = "MainMenu";
    public GameObject caseDetails;
    public GameObject  caseAccepted;
    private BaseManager baseManager;
    public GameObject Portal;

    private void Start()
    {
        baseManager = GetComponent<BaseManager>();
        caseDetails.SetActive(false);
        caseAccepted.SetActive(false);
        Portal.SetActive(false);    
    }

    public void SelectCase(string caseName)
    {
        selectedCaseName = caseName;
        caseDetails.SetActive(true);
        caseAccepted.SetActive(false);
    }

    public void ConfirmCaseSelection()
    {
        baseManager.SetLoadCaseName(selectedCaseName);
        caseDetails.SetActive(false);
        caseAccepted.SetActive(true);
        Portal.SetActive(true);

    }
}
