using System;
using Remodeling.Data;
using Remodeling.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Remodeling.UI
{
    public class UpgradeListItemUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text cost;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private GameObject lockedBadge;

        private UpgradeDefinitionSO _def;
        private Action<UpgradeDefinitionSO> _onClick;

        public void Bind(UpgradeDefinitionSO def, bool isOwned, bool canBuy, Action<UpgradeDefinitionSO> onClick)
        {
            _def = def;
            _onClick = onClick;

            PlayerUpgradeState playerState = FindFirstObjectByType<PlayerUpgradeState>();

            if (icon) icon.sprite = def ? def.icon : null;
            if (title) title.text = def ? def.displayName : "(null)";
            if (cost) cost.text = def ? def.NextCost(playerState.GetCount(def.id)).ToString() : "-";


            if (ownedBadge)
            {
                if(def.maxPurchases > 0 && playerState.GetCount(def.id) >= def.maxPurchases) 
                {              
                    ownedBadge.SetActive(true);
                }
                else
                {
                    ownedBadge.SetActive(false);
                }
            }
            if (lockedBadge) lockedBadge.SetActive(!isOwned && !canBuy);

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke(_def));
                button.interactable = def != null;
            }
        }
    }
}
