using MoreMountains.Tools;
using UnityEngine;

public class ReadyCheck : MonoBehaviour
{
    public CharacterSelect[] characterSelects;


    // Update is called once per frame
    void Update()
    {
        CheckReady();
    }

    private void CheckReady()
    {
        bool allReady = true;
        int connectedPlayers = 0;
        
        for(int i = 0; i < characterSelects.Length; i++)
        {
            if (SaveData.Current.mainData.connected[i])
            {
                connectedPlayers++;
                Debug.Log("Player " + i + " is connected");
                if (!characterSelects[i].isReady)
                {

                    Debug.Log("Player " + i + " is not ready");
                    allReady = false;
                    break;
                }
            }
        }

        if (allReady && connectedPlayers > 0)
        {

            SerializationManager.Save(SaveData.Current.saveName, SaveData.Current);
            Debug.Log("Next Scene");
            MMSceneLoadingManager.LoadScene("TestWorldSelect");
        }
    }
}
