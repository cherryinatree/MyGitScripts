using UnityEngine;
using UnityEngine.UI;
using Cherry.DayAndTime;
using TMPro;

public class DayReportPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI text;

    private void OnEnable()
    {
        if (DayTimeSystem.Instance != null)
            DayTimeSystem.Instance.OnStoreClosed += HandleClosed;
    }

    private void OnDisable()
    {
        if (DayTimeSystem.Instance != null)
            DayTimeSystem.Instance.OnStoreClosed -= HandleClosed;
    }

    private void HandleClosed(DayReport report)
    {
        if (root) root.SetActive(true);
        if (text)
        {
            text.text =
                $"Day {report.dayNumber} Report\n" +
                $"Minutes Open: {report.minutesOpen}\n" +
                $"Customers: {report.customers}\n" +
                $"Transactions: {report.transactions}\n" +
                $"Revenue: ${report.revenue:0.00}\n" +
                $"Profit: ${report.profit:0.00}\n" +
                $"{report.notes}";
        }
    }

    public void CloseReport()
    {
        if (root) root.SetActive(false);
    }
}
