using UnityEngine;
#if STEAMWORKS
using Steamworks;
#endif

public class SteamBootstrap : MonoBehaviour
{
    public static bool IsReady { get; private set; }
#if STEAMWORKS
SteamInviteBridge.RegisterJoinHandler(async (code) => {
    await LobbyServiceFacade.I.JoinByCodeAsync(code);
    NetworkBootstrap.I.LoadLobbyScene();
});
#endif
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
#if STEAMWORKS
        try { IsReady = SteamAPI.Init(); if(!IsReady) Debug.LogWarning("SteamAPI.Init failed"); }
        catch (System.Exception e) { Debug.LogError(e); }
#endif
    }

    void Update()
    {
#if STEAMWORKS 
    if(IsReady) SteamAPI.RunCallbacks(); 
        #endif 
}
    void OnDisable(){ 
        #if STEAMWORKS 
        if(IsReady) SteamAPI.Shutdown(); 
        #endif 
    }
}
