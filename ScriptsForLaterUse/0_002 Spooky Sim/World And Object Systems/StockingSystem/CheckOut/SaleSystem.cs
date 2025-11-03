using UnityEngine;
using UnityEngine.UI;

public class SaleSystem : MonoBehaviour
{
    public Transform itemInfoPrefab;
    public Transform content;
    public TMPro.TextMeshProUGUI totalPriceText;
    public TMPro.TextMeshProUGUI cashText;
    public TMPro.TextMeshProUGUI changeDueText;
    public TMPro.TextMeshProUGUI changeGivenText;
    public Transform cashRegistar;
    public GameObject completeTransactionButton;
    private CheckOutCounter checkOutCounter;

    [Header("Currency Prefabs")]
    public GameObject ones;
    public GameObject fives;
    public GameObject tens;
    public GameObject twenties;
    public GameObject pennies;
    public GameObject nickels;
    public GameObject dimes;
    public GameObject quarters;

    private float totalPrice = 0f;
    private float changeGiven = 0f;

    public string[] cardNames = { "UniVersa", "GalaxyCard", "AndromedaExpress", "Explore" };

    private ChangeStack changeStack;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClearSaleUI();
        totalPrice = 0f;
        totalPriceText.text = "Total:\n [']" + totalPrice.ToString("F2");
        cashText.text = "Cash:\n [']0.00";
        changeDueText.text = "Change Due:\n [']0.00";
        changeGivenText.text = "Change Given:\n [']" + changeGiven.ToString("F2");
        completeTransactionButton.SetActive(false);
        changeStack = GetComponent<ChangeStack>();
        checkOutCounter = GetComponent<CheckOutCounter>();
    }

    public float GetTotalPrice()
    {
        return totalPrice;
    }

    public void ResetSale()
    {
        ClearSaleUI();
        checkOutCounter.ResetCounter();
        totalPrice = 0f;
        changeGiven = 0f;
        totalPriceText.text = "Total:\n [']" + totalPrice.ToString("F2");
        cashText.text = "Cash:\n [']0.00";
        changeDueText.text = "Change Given:\n [']0.00";
        changeGivenText.text = "Change Given:\n [']" + changeGiven.ToString("F2");
    }

    public void AddItemToSaleUI(string itemName, float itemPrice)
    {
        Transform itemInfoInstance = Instantiate(itemInfoPrefab, content);
        TMPro.TextMeshProUGUI[] texts = itemInfoInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI text in texts)
        {
            if (text.name == "ItemNameText")
            {
                text.text = itemName;
            }
            else if (text.name == "ItemPriceText")
            {
                text.text = "[']" + itemPrice.ToString("F2");
            }
        }
        totalPrice += itemPrice;
        totalPriceText.text = "Total:\n [']" + totalPrice.ToString("F2");
    }

    public void UpdateCashAndChange(float cashGiven)
    {
        cashText.text = "Cash:\n [']" + cashGiven.ToString("F2");
        float change = cashGiven - totalPrice;
        changeDueText.text = "Change Due:\n [']" + change.ToString("F2");
    }

    public void PayWithCard()
    {
        
        cashText.text = "Card:\n " + cardNames[Random.Range(0,cardNames.Length-1)] + " XXXX" + Random.Range(1000,9999).ToString();

        changeDueText.text = "Card Accepted";
        changeGivenText.text = "Thank You!";
        completeTransactionButton.SetActive(true);
    }

    public void GiveChange(float amount)
    {
        changeGiven += amount;
        changeGivenText.text = "Change Given:\n [']" + changeGiven.ToString("F2");
        switch (amount)
        {
            case 0.01f:
                changeStack.PushClone(pennies);
                break;
            case 0.05f:
                changeStack.PushClone(nickels);
                break;
            case 0.1f:
                changeStack.PushClone(dimes);
                break;
            case 0.25f:
                changeStack.PushClone(quarters);
                break;
            case 1f:
                changeStack.PushClone(ones);
                break;
            case 5f:
                changeStack.PushClone(fives);
                break;
            case 10f:
                changeStack.PushClone(tens);
                break;
            case 20f:
                changeStack.PushClone(twenties);
                break;
        }
        if (changeGiven >= totalPrice)
        {
            completeTransactionButton.SetActive(true);
        }
    }

    public void TakeBackChange(float amount)
    {
        changeGiven -= amount;
        changeGivenText.text = "Change Given:\n [']" + changeGiven.ToString("F2");
        if (changeGiven < totalPrice)
        {
            completeTransactionButton.SetActive(false);
        }
    }

    public void CompleteTransaction()
    {
        if (changeDueText.text == "Card Accepted" || changeGiven >= totalPrice)
        {
            ResetSale();
        }
    }


    private void ClearSaleUI()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }
}
