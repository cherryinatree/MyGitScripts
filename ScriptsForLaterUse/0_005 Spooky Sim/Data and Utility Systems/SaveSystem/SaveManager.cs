using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public TMP_InputField saveName;
    public GameObject loadButtonPrefab;
    public Transform loadArea;

    public string[] saveFiles;

    private void Start()
    {
        loadArea.gameObject.SetActive(false); 
    }

    public void LoadScreen()
    {

        loadArea.gameObject.SetActive(true);
        ShowLoadScreen();
    }






    public void OnSave()
    {
        SerializationManager.Save(saveName.text, SaveData.Current);
    }

    public void OnLoad(string saveName)
    {
        SaveData.Current = (SaveData)SerializationManager.Load(saveName);
        Debug.Log(saveName);
        SceneManager.LoadScene(SaveData.Current.mainData.playerData.Scene);
    }

    public void GetLoadFiles()
    {
        if(!Directory.Exists(Application.persistentDataPath + "/saves/"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/saves/");
        }

        saveFiles = Directory.GetFiles(Application.persistentDataPath + "/saves/");
        Debug.Log(saveFiles.Length);
    }

    public void ShowLoadScreen()
    {
        GetLoadFiles();

        foreach (Transform button in loadArea)
        {
            Destroy(button.gameObject);
        }


        for (int i = 0; i < saveFiles.Length; i++)
        {

            GameObject buttonObject = Instantiate(loadButtonPrefab);
            buttonObject.transform.SetParent(loadArea.transform, false);

            var index = i;
            buttonObject.GetComponent<Button>().onClick.AddListener(() =>
            {

                OnLoad(saveFiles[index]);

            });
            buttonObject.GetComponentInChildren<TMP_Text>().text = saveFiles[index].Replace(Application.persistentDataPath + "/saves/", "");
        }
    }

}
