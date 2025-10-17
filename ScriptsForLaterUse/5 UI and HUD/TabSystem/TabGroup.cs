using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabGroup : MonoBehaviour
{

    public List<TabButton> tabButtons;
    public Sprite tabIdle;
    public Sprite tabHover;
    public Sprite tabSelected;
    private TabButton selectedTab;
    public GameObject[] objectsToSwap;

    private void Start()
    {
        ResetObjects();
        ResetTabs();
    }

    private void ResetObjects()
    {

        for (int i = 0; i < objectsToSwap.Length; i++)
        {
            objectsToSwap[i].SetActive(false);
        }
    }

    public void Subscribe(TabButton button)
    {
        if (tabButtons == null)
        {
            tabButtons = new List<TabButton>();
        }

        tabButtons.Add(button);
    }

    public void OnTabEnter(TabButton button)
    {
        ResetTabs();

        if (selectedTab == null || button != selectedTab)
        {
            button.background.sprite = tabHover;
        }

    }

    public void OnTabExit(TabButton button)
    {
        ResetTabs();
    }
    public void OnTabSelected(TabButton button)
    {
        selectedTab = button;
        ResetTabs();

        if (selectedTab != null && button == selectedTab)
        {
            button.background.sprite = tabSelected;
        }

        int index = button.transform.GetSiblingIndex();
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if(button == tabButtons[i])
            {
                index = i;
            }
        }
        PanelSwap(index);
    }

    private void PanelSwap(int index)
    {

        for (int i = 0; i < objectsToSwap.Length; i++)
        {
            if (i == index)
            {
                if (objectsToSwap[i].activeSelf)
                {
                    objectsToSwap[i].SetActive(false);
                }
                else
                {
                    objectsToSwap[i].SetActive(true);
                }
            }
            else
            {
                objectsToSwap[i].SetActive(false);
            }
        }
    }

    public void ResetTabs()
    {
        foreach (TabButton button in tabButtons)
        {
            if (selectedTab == null || button != selectedTab)
            {
                button.background.sprite = tabIdle;
            }
        }
    }
}
