using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_InputField lobbyName;
    [SerializeField] TMP_Dropdown visibilityDropdown; // 0: Public, 1: Private, 2: Friends
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] Button createBtn, joinBtn, quickJoinBtn;

    async void Start()
    {
        await LobbyServiceFacade.I.InitAsync(false); // anon for dev
        createBtn.onClick.AddListener(OnCreate);
        joinBtn.onClick.AddListener(OnJoinCode);
        quickJoinBtn.onClick.AddListener(OnQuickJoin);
    }

    async void OnCreate()
    {
        var vis = visibilityDropdown.value switch { 0 => "public", 1 => "private", 2 => "friends", _ => "public" };
        var name = string.IsNullOrWhiteSpace(lobbyName.text) ? "Game" : lobbyName.text;
        await LobbyServiceFacade.I.CreateLobbyAsync(name, 8, vis);
        NetworkBootstrap.I.LoadLobbyScene();
    }

    async void OnJoinCode()
    {
        if (string.IsNullOrWhiteSpace(joinCodeInput.text)) return;
        await LobbyServiceFacade.I.JoinByCodeAsync(joinCodeInput.text.Trim().ToUpper());
        NetworkBootstrap.I.LoadLobbyScene();
    }

    async void OnQuickJoin()
    {
        await LobbyServiceFacade.I.QuickJoinPublicAsync();
        NetworkBootstrap.I.LoadLobbyScene();
    }
}
