using TMPro;
using UnityEngine;

/// <summary>
/// Displays the active customer's order on a station screen/ticket.
/// Can read directly from a FoodCustomerOrder or from a FoodStationLineController.
/// </summary>
public class FoodOrderDisplayPanel : MonoBehaviour
{
    [Header("References")]
    public FoodCustomerOrder customer;
    public FoodStationLineController lineController;

    [Header("UI")]
    public TextMeshProUGUI orderText;
    public TextMeshProUGUI paymentText;
    public string noOrderText = "No order";
    public string paymentFormat = "Pays: ${0}";

    [Header("Refresh")]
    public bool refreshEveryFrame;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            Refresh();
    }

    public void Refresh()
    {
        FoodCustomerOrder targetCustomer = GetTargetCustomer();

        if (targetCustomer == null || targetCustomer.CurrentOrder == null)
        {
            if (orderText != null) orderText.text = noOrderText;
            if (paymentText != null) paymentText.text = "";
            return;
        }

        if (orderText != null)
            orderText.text = targetCustomer.CurrentOrder.GetReadableText();

        if (paymentText != null)
            paymentText.text = string.Format(paymentFormat, targetCustomer.GetPaymentAmount());
    }

    public void Clear()
    {
        if (orderText != null) orderText.text = noOrderText;
        if (paymentText != null) paymentText.text = "";
    }

    private FoodCustomerOrder GetTargetCustomer()
    {
        if (lineController != null && lineController.CurrentCustomerOrder != null)
            return lineController.CurrentCustomerOrder;

        return customer;
    }
}
