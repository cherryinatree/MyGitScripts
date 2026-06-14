using TMPro;
using UnityEngine;

/// <summary>
/// Debug/quality-of-life UI that shows what the player is currently holding.
/// Good while tuning recipes and order checks.
/// </summary>
public class FoodHeldItemInspector : MonoBehaviour
{
    public PlayerFoodHands hands;
    public TextMeshProUGUI text;
    public string emptyText = "Hands empty";
    public bool refreshEveryFrame = true;

    private void Start()
    {
        if (hands == null)
            hands = FindFirstObjectByType<PlayerFoodHands>();

        Refresh();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            Refresh();
    }

    public void Refresh()
    {
        if (text == null) return;

        if (hands == null || !hands.TryGetHeldFood(out FoodItem item) || item == null)
        {
            text.text = emptyText;
            return;
        }

        text.text = item.DebugDescription();
    }
}
