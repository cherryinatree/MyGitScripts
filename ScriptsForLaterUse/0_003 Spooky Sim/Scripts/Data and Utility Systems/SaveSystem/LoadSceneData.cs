using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LoadSceneData
{
    public LevelRunSaveData currentScene;
    public LevelRunSaveData nextScene;


    public List<LevelRunSaveData> scenesThePlayerHasBeenTo;


}
