using System;


[Serializable]
public class LevelRunSaveData
{
    public string runId;
    public string levelId;
    public string sceneName;
    public string displayName;
    public string category;

    public int rolledReward;

    public int attempts;
    public int timesFailed;
    public int timesAbandoned;
    public int timesCompleted;
}

