using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinLocations : MonoBehaviour
{
    [HideInInspector]
    public int currentLocation = 0;
    public GameObject[] locations;
    private List<PlayerOverhead> playersReady;

    private void Start()
    {
        playersReady = new List<PlayerOverhead>();   
    }

    public void AddPlayer(PlayerOverhead player)
    {
        playersReady.Add(player);
    }

    public void RemovePlayer(PlayerOverhead player)
    {
        playersReady.Remove(player);
    }

    public void StartGame()
    {
        int i = 0;
        if(playersReady.Count > 0)
        {
            foreach (PlayerOverhead player in playersReady)
            {
                if (player.isReady)
                {
                    i++;
                }
            }
            if (i == playersReady.Count)
            {
                Debug.Log("Start Game");
            }
        }
    }
}
