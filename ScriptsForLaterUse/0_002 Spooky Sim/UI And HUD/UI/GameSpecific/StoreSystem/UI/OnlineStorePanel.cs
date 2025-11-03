using TMPro;
using UnityEngine;

public class OnlineStorePanel : MonoBehaviour
{

    public GameObject icon0;
    public GameObject icon1;
    public GameObject icon2;

    public GameObject lockPanel;
    public TextMeshProUGUI lockText;
    private int requiredLevel = 5;
    private int playerLevel = 0;

    public void Setup(ShopItemDefinition item0, ShopItemDefinition item1, ShopItemDefinition item2, int requireLevel)
    {
        requiredLevel = requireLevel;
        playerLevel = SaveData.Current.mainData.playerData.level;
        if (playerLevel < requiredLevel)
        {
            lockPanel.SetActive(true);
            lockText.text = $"Unlock at Level {requiredLevel}";
        }
        else
        {
            lockPanel.SetActive(false);
        }

        if (item0 != null) 
        {
            icon0.GetComponent<StoreItemIcon>().Setup(item0);
        }
        else
        {
            icon0.SetActive(false);

        }
        if (item1 != null) 
        { 
        icon1.GetComponent<StoreItemIcon>().Setup(item1); 
        }
        else
        {
            icon1.SetActive(false);
        }
        if (item2 != null)
        {
            icon2.GetComponent<StoreItemIcon>().Setup(item2);
        }
        else
        {
            icon2.SetActive(false);
        }
    }

}
