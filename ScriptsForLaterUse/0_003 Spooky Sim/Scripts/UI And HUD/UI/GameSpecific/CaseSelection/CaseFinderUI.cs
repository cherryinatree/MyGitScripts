using Cherry.Levels;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CaseFinderUI : MonoBehaviour
{

    public LevelCatalogue levelCatalogue;
    public Transform caseList;
    public GameObject prefabPanel;

    public GameObject caseDetails;
    public GameObject caseAccepted;
    public GameObject Portal;

    public TextMeshProUGUI caseNameText;
    public TextMeshProUGUI caseDescriptionText;


    private void Start()
    {

        caseDetails.SetActive(false);
        caseAccepted.SetActive(false);
        Portal.SetActive(false);
    }

    private void OnEnable()
    {
        //int playerLevel = SaveData.Current.mainData.playerData.level;
        // For testing purposes, set player level to 0
        int playerLevel = 0;
        foreach (Transform child in caseList)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < levelCatalogue.levels.Count; i++)
        {
            LevelDefinition levelDef = levelCatalogue.levels[i];
            Instantiate(prefabPanel, caseList).GetComponent<CaseFinderPanel>().Setup(levelDef, playerLevel, this);
        }
    }

    public void SelectCase(LevelDefinition levelDef)
    {

        SaveData.Current.mainData.loadSceneData.nextScene = levelDef.ConvertToLevelRunSaveData();
        caseDetails.SetActive(true);
        caseAccepted.SetActive(false);

        caseNameText.text = levelDef.displayName;
        caseDescriptionText.text = levelDef.description;
    }

    public void ConfirmCaseSelection()
    {
        caseDetails.SetActive(false);
        caseAccepted.SetActive(true);
        Portal.SetActive(true);

    }

}
