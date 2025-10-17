using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    private string coin100 = "com.john.digikong.coin100";
    private string coin750 = "com.john.digikong.coin750";
    private string adLimit = "com.john.digikong.adlimter";
    public GameObject restoreButton;

    
    private void Awake()
    {
        if(Application.platform != RuntimePlatform.IPhonePlayer)
        {
            restoreButton.SetActive(false);
        }

        if (CodelessIAPStoreListener.Instance.GetProduct("com.john.digikong.adlimter").receipt != null)
        {

            GameSingleton.Instance.save.Core.AdFree = true;
        }
    }

    

    public void OnPurchaceComplete(Product product)
    {
        if (product.definition.id == coin100)
        {
            GameSingleton.Instance.save.Core.Gold += 100;
            SaveAdjuster.Save();
        }
        if (product.definition.id == coin750)
        {
            GameSingleton.Instance.save.Core.Gold += 750;
            SaveAdjuster.Save();
        }
        if (product.definition.id == adLimit)
        {
            GameSingleton.Instance.save.Core.AdFree = true;
            SaveAdjuster.Save();
        }
    }

    public void OnPurchaceFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log(product.definition.id + " Failed Because " + failureReason.ToString());
    }
}
