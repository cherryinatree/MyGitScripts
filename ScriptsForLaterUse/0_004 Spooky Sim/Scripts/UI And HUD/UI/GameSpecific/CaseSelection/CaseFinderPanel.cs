using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaseFinderPanel : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI caseName;
    public TextMeshProUGUI caseDescription;
    private LevelDefinition levelDef;

    private CaseFinderUI caseFinderUI;

    public void Setup(LevelDefinition levelDef, int playerLevel, CaseFinderUI caseFinder)
    {
        caseFinderUI = caseFinder;
        icon.sprite = levelDef.thumbnail;
        caseName.text = levelDef.displayName;
        caseDescription.text = levelDef.description;
        this.levelDef = levelDef;
        // You can add more setup logic here, such as locking the case if the player level is too low
    }

    public void OnClickOpenCase()
    {
        caseFinderUI.SelectCase(levelDef);
    }
}
