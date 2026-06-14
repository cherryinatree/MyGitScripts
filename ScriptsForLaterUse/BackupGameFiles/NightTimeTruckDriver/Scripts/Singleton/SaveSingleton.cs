using UnityEngine;

public class SaveSingleton
{
    private static SaveSingleton _instance;
    public PlayerData truckStats;

    public static SaveSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SaveSingleton();
            }

            if(_instance.truckStats == null)
            {
                _instance.Load(); // Load truck stats if not already loaded
            }
            return _instance;
        }
    }

    private void Load()
    {
        truckStats = (PlayerData)SerializationManager.LoadWithJustTheSaveName("main2"); // Load truck stats at the start

        // Initialize truckStats if it hasn't been set
        if (truckStats == null)
        {
            truckStats = new PlayerData();
        }
    }


    // Prevent outside instantiation
    private SaveSingleton() { }

}
