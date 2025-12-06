using System.Collections.Generic;
using System.Text;
using Remodeling.Data;
using Remodeling.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Remodeling.UI
{
    public class RemodelShopWindow : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private UpgradeCatalogSO catalog;
        [SerializeField] private PlayerUpgradeState playerState;
        [SerializeField] private ShipRemodelController remodelController;

        [Header("Tabs")]
        [SerializeField] private Button tabSize;
        [SerializeField] private Button tabFeature;
        [SerializeField] private Button tabAutomation;
        [SerializeField] private Button tabDecor;
        [SerializeField] private Button tabUtility;

        [Header("Equipment Tabs")]
        [SerializeField] private Button tabCollector;
        [SerializeField] private Button tabArHeadset;
        [SerializeField] private Button tabRig;
        [SerializeField] private Button tabMisc;

        [Header("List")]
        [SerializeField] private Transform listContent;
        [SerializeField] private UpgradeListItemUI listItemPrefab;

        [Header("Details Panel")]
        [SerializeField] private Image detailsIcon;
        [SerializeField] private TMP_Text detailsTitle;
        [SerializeField] private TMP_Text detailsDesc;
        [SerializeField] private TMP_Text detailsReqs;
        [SerializeField] private TMP_Text detailsCost;
        [SerializeField] private Button previewButton;
        [SerializeField] private TMP_Text previewButtonLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonLabel;

        [Header("Top Bar")]
        [SerializeField] private TMP_Text creditsText;

        private readonly List<UpgradeListItemUI> _spawned = new();
        private UpgradeCategory _currentCategory = UpgradeCategory.Size;
        private UpgradeDefinitionSO _selected;

        private bool _previewing;
        private UpgradeDefinitionSO _previewDef;
        private List<IRemodelUndo> _previewUndos;



        private void OnEnable()
        {
            WireTabs();
            playerState.OnChanged += RefreshCredits;

            // Ensure scene reflects owned upgrades when window opens.
            //remodelController.ApplyBaseline(playerState);

            SetCategory(UpgradeCategory.Size);
            RefreshCredits();
        }

        private void OnDisable()
        {
            playerState.OnChanged -= RefreshCredits;
            StopPreviewIfAny();
        }

        private void WireTabs()
        {
            if(tabSize) tabSize.onClick.RemoveAllListeners();
            if (tabFeature) tabFeature.onClick.RemoveAllListeners();
            if (tabAutomation) tabAutomation.onClick.RemoveAllListeners();
            if (tabDecor) tabDecor.onClick.RemoveAllListeners();
            if (tabUtility) tabUtility.onClick.RemoveAllListeners();

            if (tabSize) tabSize.onClick.AddListener(() => SetCategory(UpgradeCategory.Size));
            if (tabFeature) tabFeature.onClick.AddListener(() => SetCategory(UpgradeCategory.Feature));
            if (tabAutomation) tabAutomation.onClick.AddListener(() => SetCategory(UpgradeCategory.Automation));
            if (tabDecor) tabDecor.onClick.AddListener(() => SetCategory(UpgradeCategory.Decor));
            if (tabUtility) tabUtility.onClick.AddListener(() => SetCategory(UpgradeCategory.Utility));



            if (tabCollector) tabCollector.onClick.RemoveAllListeners();
            if (tabArHeadset) tabArHeadset.onClick.RemoveAllListeners();
            if (tabRig) tabRig.onClick.RemoveAllListeners();
            if (tabMisc) tabMisc.onClick.RemoveAllListeners();

            if (tabCollector) tabCollector.onClick.AddListener(() => SetCategory(UpgradeCategory.Collector));
            if (tabArHeadset) tabArHeadset.onClick.AddListener(() => SetCategory(UpgradeCategory.ArHeadset));
            if (tabRig) tabRig.onClick.AddListener(() => SetCategory(UpgradeCategory.Rig));
            if (tabMisc) tabMisc.onClick.AddListener(() => SetCategory(UpgradeCategory.Misc));

            if (previewButton)
            {
                previewButton.onClick.RemoveAllListeners();
                previewButton.onClick.AddListener(TogglePreview);
            }
            if (buyButton)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(BuySelected);
            }
        }

        private void RefreshCredits()
        {
            if (creditsText) creditsText.text = $"Credits: {playerState.Credits}";
            RefreshDetailsButtons();
        }

        private void SetCategory(UpgradeCategory cat)
        {
            _currentCategory = cat;
            StopPreviewIfAny();
            RebuildList();
            SelectFirstAvailable();
        }

        private void RebuildList()
        {
            // Hide old
            foreach (var it in _spawned) if (it) it.gameObject.SetActive(false);

            int index = 0;
            foreach (var def in catalog.GetByCategory(_currentCategory))
            {
                if (!def) continue;

                var item = GetOrCreateItem(index++); 
                int ownedCount = playerState.GetCount(def.id);
                bool owned = ownedCount > 0;
                bool canBuy = CanPurchase(def);

                item.gameObject.SetActive(true);
                item.Bind(def, owned, canBuy, Select);
            }

            // Hide extras
            for (int i = index; i < _spawned.Count; i++)
                if (_spawned[i]) _spawned[i].gameObject.SetActive(false);
        }

        private UpgradeListItemUI GetOrCreateItem(int index)
        {
            while (_spawned.Count <= index)
            {
                var inst = Instantiate(listItemPrefab, listContent);
                _spawned.Add(inst);
            }
            return _spawned[index];
        }

        private void SelectFirstAvailable()
        {
            foreach (var def in catalog.GetByCategory(_currentCategory))
            {
                if (!def) continue;
                Select(def);
                return;
            }
            Select(null);
        }

        private void Select(UpgradeDefinitionSO def)
        {
            _selected = def;

            if (!def)
            {
                if (detailsTitle) detailsTitle.text = "No upgrade";
                if (detailsDesc) detailsDesc.text = "";
                if (detailsReqs) detailsReqs.text = "";
                if (detailsCost) detailsCost.text = "";
                if (detailsIcon) detailsIcon.sprite = null;
                RefreshDetailsButtons();
                return;
            }

            if (detailsIcon) detailsIcon.sprite = def.icon;
            if (detailsTitle) detailsTitle.text = def.displayName;
            if (detailsDesc) detailsDesc.text = def.description;
            if (detailsCost) detailsCost.text = $"Cost: {def.NextCost(playerState.GetCount(def.id))}";

            if (detailsReqs)
            {
                var sb = new StringBuilder();
                if (def.requiredUpgradeIds != null && def.requiredUpgradeIds.Length > 0)
                {
                    sb.AppendLine("Requires:");
                    foreach (var r in def.requiredUpgradeIds)
                        sb.AppendLine($"• {r}");
                }
                else sb.Append("Requires: (none)");
                detailsReqs.text = sb.ToString();
            }

            RefreshDetailsButtons();
        }

        private bool RequirementsMet(UpgradeDefinitionSO def)
        {
            if (def.requiredUpgradeIds == null) return true;
            foreach (var r in def.requiredUpgradeIds)
                if (!playerState.Owns(r))
                    return false;
            return true;
        }
        /*
                private bool CanPurchase(UpgradeDefinitionSO def)
                {
                    if (!def) return false;
                    if (def.unique && playerState.Owns(def.id)) return false;
                    if (!RequirementsMet(def)) return false;
                    if (playerState.Credits < def.cost) return false;
                    return true;
                }*/

        private bool CanPurchase(UpgradeDefinitionSO def)
        {
            if (!def) return false;

            int ownedCount = playerState.GetCount(def.id);
            if (def.IsMaxed(ownedCount)) return false;
            if (!RequirementsMet(def)) return false;

            int price = def.NextCost(ownedCount);
            if (playerState.Credits < price) return false;

            return true;
        }


        private void RefreshDetailsButtons()
        {
            if (!previewButton || !buyButton) return;

            bool hasSelection = _selected != null; 
            int ownedCount = playerState.GetCount(_selected.id);
            bool owned = hasSelection && ownedCount > 0;
            //bool owned = hasSelection && playerState.Owns(_selected.id);
            bool canBuy = hasSelection && CanPurchase(_selected);

            previewButton.interactable = hasSelection;

            if (buyButtonLabel)
            {
                if (!hasSelection) buyButtonLabel.text = "Buy";
                else if (owned) buyButtonLabel.text = "Owned";
                else if (!RequirementsMet(_selected)) buyButtonLabel.text = "Locked";
                else if (playerState.Credits < _selected.NextCost(playerState.GetCount(_selected.id))) buyButtonLabel.text = "Not enough";
                else buyButtonLabel.text = "Buy";
            }

            buyButton.interactable = hasSelection && !owned && canBuy;

            if (previewButtonLabel)
            {
                previewButtonLabel.text = (_previewing && _previewDef == _selected) ? "Stop Preview" : "Preview";
            }
        }

        private void TogglePreview()
        {
            if (_selected == null) return;

            if (_previewing && _previewDef == _selected)
            {
                StopPreviewIfAny();
                return;
            }

            StopPreviewIfAny();

            // Start preview on top of baseline
            _previewDef = _selected;
            _previewUndos = remodelController.ApplyUpgradeWithUndo(playerState, _previewDef);
            _previewing = true;

            RefreshDetailsButtons();
        }

        private void StopPreviewIfAny()
        {
            if (!_previewing) return;

            if (_previewUndos != null)
            {
                for (int i = _previewUndos.Count - 1; i >= 0; i--)
                    _previewUndos[i]?.Undo();
            }

            _previewUndos = null;
            _previewDef = null;
            _previewing = false;

            // Re-apply baseline just to be safe (handles any weird interactions)
            remodelController.ApplyBaseline(playerState);

            RefreshDetailsButtons();
            RebuildList();
        }
        /*
        private void BuySelected()
        {
            if (_selected == null) return;
            if (!CanPurchase(_selected)) return;

            StopPreviewIfAny();

            if (!playerState.TrySpend(_selected.cost)) return;
            playerState.TryAddOwned(_selected.id);

            // Apply permanently (baseline re-apply ensures it sticks)
            remodelController.ApplyBaseline(playerState);

            RefreshCredits();
            RebuildList();
            RefreshDetailsButtons();

            // If you want: playerState.WriteToSave();
        }*/
        private void BuySelected()
        {
            if (_selected == null) return;

            int ownedCount = playerState.GetCount(_selected.id);
            if (_selected.IsMaxed(ownedCount)) return;
            if (!RequirementsMet(_selected)) return;

            StopPreviewIfAny();

            int price = _selected.NextCost(ownedCount);
            if (!playerState.TrySpend(price)) return;

            playerState.AddPurchase(_selected.id, 1);

            remodelController.ApplyBaseline(playerState);

            RefreshCredits();
            RebuildList();
            RefreshDetailsButtons();
            Debug.Log($"Purchased upgrade {_selected.displayName} for {price} credits.");
            //_selected = null;
        }

    }
}
