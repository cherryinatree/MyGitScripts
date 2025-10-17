using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinTrigger : MonoBehaviour
{

    public float bonusTime = 0;

    private GameObject endGoal;
    private GameObject[] spawners;
    private GameObject[] enemies;
    private GameObject[] mobs;
    private Player player;

    public GameObject Boss;

    public bool isBossRound = false;

    bool isWin;

    float moveBack = 1000;

    private void Start()
    {
        endGoal = GameObject.Find("EndGoal");
        spawners = GameObject.FindGameObjectsWithTag("Spawner");
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        isWin = false;
    }

    private void FixedUpdate()
    {
        if (isWin)
        {
            for (int i = 0; i < moveBack; i++)
            {

                endGoal.transform.position = new Vector3(endGoal.transform.position.x,
                    endGoal.transform.position.y, endGoal.transform.position.z + 0.00001f);
            }
        }
        if (isBossRound)
        {
            if (Boss == null)
            {

                WinLoss.WinCondition(bonusTime);
                WinCondition();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            WinLoss.WinCondition(bonusTime);
            WinCondition();
        }
    }

    private void WinCondition()
    {
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        mobs = GameObject.FindGameObjectsWithTag("MOB");
        foreach (GameObject spawn in spawners)
        {
            spawn.SetActive(false);
        }
        foreach (GameObject spawn in enemies)
        {
            spawn.SetActive(false);
        }
        foreach (GameObject spawn in mobs)
        {
            spawn.SetActive(false);
        }
        player.winPause = true;
        isWin = true;
    }

}
