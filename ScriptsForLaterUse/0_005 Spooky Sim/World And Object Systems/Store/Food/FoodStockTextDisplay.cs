using TMPro;
using UnityEngine;

/// <summary>
/// Displays one stock value from a FoodStationInventory.
/// Good for ice cream bin signs, dough amount, topping refill amount, etc.
/// </summary>
public class FoodStockTextDisplay : MonoBehaviour
{
    public FoodStationInventory inventory;
    public FoodStockType stockType;
    public TextMeshProUGUI text;
    public string format = "{0}: {1}";
    public bool useFriendlyName = true;
    public bool refreshEveryFrame = true;

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
        if (text == null || inventory == null) return;

        string displayName = useFriendlyName ? SplitCamelCase(stockType.ToString()) : stockType.ToString();
        text.text = string.Format(format, displayName, inventory.GetAmount(stockType));
    }

    private string SplitCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(input.Length + 8);
        builder.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            char current = input[i];
            if (char.IsUpper(current)) builder.Append(' ');
            builder.Append(current);
        }

        return builder.ToString();
    }
}
