using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

        [Header("Debounce")]
        [Tooltip("Prevents the report from closing immediately if the same key press that closed the store also counts as 'close report'.")]
        [SerializeField] private bool waitForReleaseIfHeldOnOpen = true;

        private bool _isOpen;

        // Debounce state
        private int _openedFrame = -1;
        private bool _waitForRelease;

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
            if (a != null)
            {
                // If the action isn't enabled because its action map is off, this makes it work.
                // If it's already enabled, Enable() is harmless.
                a.Enable();
                a.performed += OnClosePerformed;
            }
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

            // Ignore the exact frame we opened (prevents instant close)
            if (Time.frameCount == _openedFrame) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            var key = kb[closeKey];
            if (key == null) return;

            // If the key was already held when the report opened, wait until it's released once.
            if (_waitForRelease)
            {
                if (!key.isPressed) _waitForRelease = false;
                return;
            }

            if (key.wasPressedThisFrame)
                CloseReport();
        }

        private void HandleStoreClosed(DayReport report)
        {
            _isOpen = true;
            _openedFrame = Time.frameCount;

            var kb = Keyboard.current;
            if (waitForReleaseIfHeldOnOpen && kb != null)
            {
                var key = kb[closeKey];
                _waitForRelease = (key != null && key.isPressed);
            }
            else
            {
                _waitForRelease = false;
            }

            Debug.Log("Showing store report for day " + report.dayNumber);

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
                    $"Press {closeKey} to close";
            }
        }

        private void OnClosePerformed(InputAction.CallbackContext _)
        {
            // Same “open frame” guard for the action callback too.
            if (!_isOpen) return;
            if (Time.frameCount == _openedFrame) return;

            Debug.Log("Close report action performed");
            CloseReport();
        }

        public void CloseReport()
        {
            Debug.Log("Closing store report panel");
            _isOpen = false;
            _waitForRelease = false;

            if (root != null) root.SetActive(false);
        }
    }
}
