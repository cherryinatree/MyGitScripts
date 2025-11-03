using UnityEngine;

public class BaseManager : MonoBehaviour
{

    private string loadCaseName = "MainMenu";
    private bool caseSelected = false;




    public void SetLoadCaseName(string caseName)
    {
        loadCaseName = caseName;
        caseSelected = true;
    }

    public void ResetLoadCaseSelection()
    {
        loadCaseName = "MainMenu";
        caseSelected = false;
    }

    public void LoadScene()
    {
        if (caseSelected)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(loadCaseName);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
