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

    
    [Header("Currency parents")]
    public Transform onesParent;
    public Transform fivesParent;
    public Transform tensParent;
    public Transform twentiesParent;
    public Transform penniesParent;
    public Transform nickelsParent;
    public Transform dimesParent;
    public Transform quartersParent;

    private float totalPrice = 0f;
    private float changeGiven = 0f;
    private float cashGiven = 0f;

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
        changeStack.Clear();
        totalPrice = 0f;
        changeGiven = 0f;
        cashGiven = 0f;
        totalPriceText.text = "Total:\n [']" + totalPrice.ToString("F2");
        cashText.text = "Cash:\n [']0.00";
        changeDueText.text = "Change Given:\n [']0.00";
        changeGivenText.text = "Change Given:\n [']" + changeGiven.ToString("F2");
        completeTransactionButton.SetActive(false);
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
        this.cashGiven = cashGiven;
        cashText.text = "Cash:\n [']" + cashGiven.ToString("F2");
        float change = this.cashGiven - totalPrice;
        changeDueText.text = "Change Due:\n [']" + change.ToString("F2");
        if (0 <= totalPrice - cashGiven + changeGiven)
        {
            completeTransactionButton.SetActive(true);
        }
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
                changeStack.PushClone(pennies, penniesParent);
                break;
            case 0.05f:
                changeStack.PushClone(nickels, nickelsParent);
                break;
            case 0.1f:
                changeStack.PushClone(dimes, dimesParent);
                break;
            case 0.25f:
                changeStack.PushClone(quarters, quartersParent);
                break;
            case 1f:
                changeStack.PushClone(ones, onesParent);
                break;
            case 5f:
                changeStack.PushClone(fives, fivesParent);
                break;
            case 10f:
                changeStack.PushClone(tens, tensParent);
                break;
            case 20f:
                changeStack.PushClone(twenties, twentiesParent);
                break;
        }
        if (0 <= totalPrice - cashGiven + changeGiven)
        {
            completeTransactionButton.SetActive(true);
        }
    }

    public void TakeBackChange(float amount)
    {
        changeGiven -= amount;
        changeGivenText.text = "Change Given:\n [']" + changeGiven.ToString("F2");
        if (0 > totalPrice - cashGiven + changeGiven)
        {
            completeTransactionButton.SetActive(false);
        }
    }

    public void CompleteTransaction()
    {
        if (changeDueText.text == "Card Accepted" || changeGiven+cashGiven >= totalPrice)
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
