using TMPro;
using UnityEngine;

public class Currency : MonoBehaviour
{
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI woodText;
    private TextMeshProUGUI stoneText;
    private TextMeshProUGUI crystalText;
    private TextMeshProUGUI gemsText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        goldText = transform.Find("Gold").Find("GoldText").GetComponent<TextMeshProUGUI>();
        woodText = transform.Find("Wood").Find("WoodText").GetComponent<TextMeshProUGUI>();
        stoneText = transform.Find("Stone").Find("StoneText").GetComponent<TextMeshProUGUI>();
        crystalText = transform.Find("Crystal").Find("CrystalText").GetComponent<TextMeshProUGUI>();
        gemsText = transform.Find("Gems").Find("GemsText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCurrencyText();
    }

    private void UpdateCurrencyText()
    {
        goldText.text = SaveData.Current.mainData.beanCounter.gold.ToString();
        woodText.text = SaveData.Current.mainData.beanCounter.wood.ToString();
        stoneText.text = SaveData.Current.mainData.beanCounter.stone.ToString();
        crystalText.text = SaveData.Current.mainData.beanCounter.crystal.ToString();
        gemsText.text = SaveData.Current.mainData.beanCounter.gems.ToString();
    }
}
