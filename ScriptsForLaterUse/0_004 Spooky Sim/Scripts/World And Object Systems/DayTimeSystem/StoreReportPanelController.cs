using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.InputSystem.Controls;

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Store Report Panel Controller (Input System + TMP)")]
    public class StoreReportPanelController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DayTimeSystem dayTime;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text body;

        [Header("Input (optional Action Reference)")]
        [Tooltip("If assigned, this action will also close the report (Button binding e.g. <Keyboard>/e).")]
        [SerializeField] private InputActionReference closeReportAction;

        [Header("Fallback Key (always works)")]
        [SerializeField] private Key closeKey = Key.E;

        private bool _isOpen;

        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (root != null) root.SetActive(false);
        }

        private void OnEnable()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime != null) dayTime.OnStoreClosed += HandleStoreClosed;

            var a = closeReportAction != null ? closeReportAction.action : null;
            if (a != null) a.performed += OnClosePerformed; // don't rely on enabling/disabling maps here
        }

        private void OnDisable()
        {
            if (dayTime != null) dayTime.OnStoreClosed -= HandleStoreClosed;

            var a = closeReportAction != null ? closeReportAction.action : null;
            if (a != null) a.performed -= OnClosePerformed;
        }

        private void Update()
        {
            if (!_isOpen) return;

            // Fallback: closes even if action maps are off
           /* var kb = Keyboard.current;
            if (kb != null)
            {
                KeyControl key = closeKey switch
                {
                    Key.E => kb.eKey,
                    Key.Escape => kb.escapeKey,
                    Key.Space => kb.spaceKey,
                    _ => kb.eKey
                };

                if (key != null && key.wasPressedThisFrame)
                {
                    CloseReport();
                }
            }*/
        }

        private void HandleStoreClosed(DayReport report)
        {
            _isOpen = true;
            Debug.Log("Showing store report for day " + report.dayNumber);
            Debug.Log(root);
            if (root != null) root.SetActive(true);

            if (body != null)
            {
                body.text =
                    $"Day {report.dayNumber} Report\n" +
                    $"Time Open: {report.minutesOpen} min\n\n" +
                    $"Customers: {report.customers}\n" +
                    $"Transactions: {report.transactions}\n" +
                    $"Revenue: ${report.revenue:0.00}\n" +
                    $"Profit:  ${report.profit:0.00}\n\n" +
                    $"{report.notes}\n\n" +
                    $"Press (Mouse Left Click) to close";
            }
        }

        private void OnClosePerformed(InputAction.CallbackContext _)
        {
            Debug.Log("Close report action performed");
            if (!_isOpen) return;
            CloseReport();
        }

        public void CloseReport()
        {
            Debug.Log("Closing store report panel");
            _isOpen = false;
            if (root != null) root.SetActive(false);
        }
    }
}
