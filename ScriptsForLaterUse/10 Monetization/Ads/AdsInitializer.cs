using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] string _androidGameId;
    [SerializeField] string _iOSGameId;
    [SerializeField] bool _testMode = true;
    private string _gameId;

    [SerializeField] RewardedAdsButton rewardedAdsButton;
    [SerializeField] RewardedAdsButton2 rewardedAdsButton2;
    [SerializeField] rewardedAdsButtonLevelChange _rewardedAdsButtonLevelChange;
    [SerializeField] InterstitialAdsButton interstitialAdsButon;

    void Awake()
    {
        
        InitializeAds();
    }

    public void InitializeAds()
    {
        _gameId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSGameId
            : _androidGameId;
        Advertisement.Initialize(_gameId, _testMode, this);


    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        if(rewardedAdsButton!=null)
            rewardedAdsButton.LoadAd();
        if (rewardedAdsButton2 != null)
            rewardedAdsButton2.LoadAd();
        if (interstitialAdsButon != null)
            interstitialAdsButon.LoadAd();
        if (_rewardedAdsButtonLevelChange != null)
            _rewardedAdsButtonLevelChange.LoadAd();

    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }
}
