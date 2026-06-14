using TMPro;
using UnityEngine;

namespace ProcGen.Anomalies
{
    [DisallowMultipleComponent]
    public class AnomalyRoomInfoTMPDisplay : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AnomalyRoomGenerator generator;

        [Header("TextMeshPro UI")]
        [Tooltip("Optional all-in-one text block.")]
        [SerializeField] private TMP_Text combinedText;

        [Tooltip("Optional separate text field for the room name.")]
        [SerializeField] private TMP_Text roomNameText;

        [Tooltip("Optional separate text field for Clean / Anomaly / End Room.")]
        [SerializeField] private TMP_Text roomKindText;

        [Tooltip("Optional separate text field for correct streak.")]
        [SerializeField] private TMP_Text streakText;

        [Tooltip("Optional separate text field for generated room count.")]
        [SerializeField] private TMP_Text roomCountText;

        [Tooltip("Optional separate text field for generator status.")]
        [SerializeField] private TMP_Text statusText;

        [Header("Display Options")]
        [Tooltip("Turn this off if this UI is for the player and should not reveal whether the room is clean or anomaly.")]
        [SerializeField] private bool showHiddenRoomTruth = true;

        [SerializeField] private string noRoomText = "No room generated yet";
        [SerializeField] private string hiddenRoomKindText = "Unchecked";

        private void Awake()
        {
            if (!generator)
            {
#if UNITY_2023_1_OR_NEWER
                generator = FindFirstObjectByType<AnomalyRoomGenerator>();
#else
                generator = FindObjectOfType<AnomalyRoomGenerator>();
#endif
            }
        }

        private void OnEnable()
        {
            if (generator)
                generator.OnCurrentRoomInfoChanged += HandleRoomInfoChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (generator)
                generator.OnCurrentRoomInfoChanged -= HandleRoomInfoChanged;
        }

        public void Refresh()
        {
            if (!generator)
            {
                SetAllText("No AnomalyRoomGenerator assigned.", "None", "0", "0", "Missing Generator");
                return;
            }

            HandleRoomInfoChanged(generator.GetCurrentRoomInfo());
        }

        private void HandleRoomInfoChanged(AnomalyRoomGenerator.CurrentRoomInfo info)
        {
            string roomName = info.HasRoom ? info.roomName : noRoomText;
            string roomKind = info.HasRoom ? info.roomKind : "None";

            if (!showHiddenRoomTruth && info.HasRoom)
                roomKind = hiddenRoomKindText;

            string streak = info.correctStreak.ToString();
            string count = info.generatedContentRoomCount.ToString();
            string status = info.generationInProgress ? "Generating..." : "Ready";

            SetAllText(roomName, roomKind, streak, count, status);
        }

        private void SetAllText(string roomName, string roomKind, string streak, string count, string status)
        {
            if (combinedText)
            {
                combinedText.text =
                    $"Room: {roomName}\n" +
                    $"Type: {roomKind}\n" +
                    $"Correct Streak: {streak}\n" +
                    $"Generated Rooms: {count}\n" +
                    $"Status: {status}";
            }

            if (roomNameText)
                roomNameText.text = roomName;

            if (roomKindText)
                roomKindText.text = roomKind;

            if (streakText)
                streakText.text = "Room: " + streak + "/" + generator.streakToRevealEnd;

            if (roomCountText)
                roomCountText.text = count;

            if (statusText)
                statusText.text = status;
        }
    }
}