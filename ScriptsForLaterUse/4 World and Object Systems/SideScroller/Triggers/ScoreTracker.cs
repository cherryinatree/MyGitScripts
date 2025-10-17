using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreTracker : MonoBehaviour
{
    private float timer = 0;
    public float TimerTickAt = 1;

    GameObject WinPanel;
    GameObject LossPanel;
    GameObject PausePanel;

    // Start is called before the first frame update
    void Start()
    {
        GameSingleton.Instance.save.Score = 100;
        WinPanel = GameObject.Find("PanelWin");
        LossPanel = GameObject.Find("PanelLoss");
        PausePanel = GameObject.Find("PanelPauseMenu");
    }

    // Update is called once per frame
    void Update()
    {
        if (PanelOpener.Panels[0].activeSelf == false && PanelOpener.Panels[1].activeSelf == false && 
            PanelOpener.Panels[2].activeSelf == false)
        {
            timer += Time.deltaTime;

            if (timer > TimerTickAt)
            {
                timer = 0;
                GameSingleton.Instance.save.Score -= 1;
            }
        }
    }
}
