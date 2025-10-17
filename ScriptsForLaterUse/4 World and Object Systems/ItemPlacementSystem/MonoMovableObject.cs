using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MonoMovableObject : MonoBehaviour
{
    public MovableObject myMovableObject;

    private void OnLevelWasLoaded(int level)
    {
        GameObject.Destroy(gameObject);   
    }



    public void NewMovableObject(BuildObject build)
    {
        myMovableObject = new MovableObject();
        myMovableObject.buildObjectID = build.buildID;
        myMovableObject.rotation = Quaternion.identity;
        myMovableObject.position = transform.position;
        myMovableObject.items = new List<Item>();
        myMovableObject.lastCollectionTime = CurrentGameTime.Current().currentTimeFloat;

        myMovableObject.id = ((CurrentGameTime.Current().currentTimeFloat + (float)System.DateTime.Now.Millisecond + 
            transform.position.x + transform.position.y + transform.position.z) * (CurrentGameTime.Current().currentTimeFloat + 
            (float)System.DateTime.Now.Millisecond + transform.position.x + transform.position.y + transform.position.z)).ToString();

        UpdateMyCondition();
    }

    public void UpdateMyCondition()
    {
        bool found = false;
        if (SaveData.Current.mainData.movableObjects == null) SaveData.Current.mainData.movableObjects = new List<MovableObject>();
        for (int i = 0; i < SaveData.Current.mainData.movableObjects.Count; i++)
        {
            if (SaveData.Current.mainData.movableObjects[i].id == myMovableObject.id)
            {
                found = true;
                SaveData.Current.mainData.movableObjects[i] = myMovableObject;
                break;
            }
        }

        if (!found)
        {
            SaveData.Current.mainData.movableObjects.Add(myMovableObject);
        }
    }
}
