using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SerializationManager
{
    public static bool Save(string folderName, string saveName, string extention, object saveData)
    {
        BinaryFormatter formatter = GetBinaryFormatter();

        if (!Directory.Exists(Application.persistentDataPath + "/" +folderName))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/"+ folderName);
            Debug.Log("Save.CreateDirectory");
        }

        string path = Application.persistentDataPath + "/" + folderName + "/" + saveName + "."+ extention;

        FileStream file = File.Create(path);
        formatter.Serialize(file, saveData);
        file.Close();
        return true;
    }


    public static bool Save(string saveName, object saveData)
    {
        BinaryFormatter formatter = GetBinaryFormatter();

        if(!Directory.Exists(Application.persistentDataPath + "/saves"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/saves");
            Debug.Log("Save.CreateDirectory");
        }

        string path = Application.persistentDataPath + "/saves/" + saveName + ".save";

        FileStream file = File.Create(path);
        formatter.Serialize(file, saveData);
        file.Close();
        return true;
    }

    public static object Load(string path)
    {

        if (!File.Exists(path))
        {
            return null;
        }

        BinaryFormatter formatter = GetBinaryFormatter();

        FileStream file = File.Open(path, FileMode.Open);

        try
        {
            object save = formatter.Deserialize(file);
            file.Close();
            return save;
        }
        catch
        {
            Debug.LogErrorFormat("Failed to load file at {0}", path);
            file.Close();
            return null;
        }
    }


    public static List<MapData> LoadAllMapData(string path, string extention)
    {

        var info = new DirectoryInfo(path);

        if (info.GetFiles().Length <= 0)
        {
            return null;
        }

        BinaryFormatter formatter = GetBinaryFormatter();
        List<MapData> save = new List<MapData>();

        foreach (var aFile in info.GetFiles())
        {
            if (aFile.Extension == extention)
            {

                FileStream file = File.Open(aFile.FullName, FileMode.Open);
                save.Add((MapData)formatter.Deserialize(file));
                file.Close();
            }
        }


        return save;
        /*
        try
        {
            object save = formatter.Deserialize(file);
            file.Close();
            return save;
        }
        catch
        {
            Debug.LogErrorFormat("Failed to load file at {0}", path);
            file.Close();
            return null;
        }*/
    }

    public static void Delete(string saveName)
    {

        string path = Application.persistentDataPath + "/saves/" + saveName;
        if (!File.Exists(path))
        {
            return;
        }
        else
        {

            File.Delete(path);
        }
    }

    public static void Delete(string folderName, string saveName)
    {

        string path = Application.persistentDataPath + "/"+ folderName + "/" + saveName;
        if (!File.Exists(path))
        {
            return;
        }
        else
        {

            File.Delete(path);
        }
    }

    public static string[] LoadAllSaveNames()
    {
        string path = Application.persistentDataPath + "/saves/";
        /*if (!File.Exists(path))
        {
            Debug.Log("No path");
            return null;
        }*/


        string[] names = Directory.GetFiles(path);
        for (int i = 0; i < names.Length; i++)
        {

            names[i] = names[i].Substring(path.ToCharArray().Length, (names[i].ToCharArray().Length - path.ToCharArray().Length));
        }


        return names;
    }
    public static string[] LoadAllSaveNames(string folder)
    {
        string path = Application.persistentDataPath + "/"+ folder + "/";
        /*if (!File.Exists(path))
        {
            Debug.Log("No path");
            return null;
        }*/


        string[] names = Directory.GetFiles(path);
        for (int i = 0; i < names.Length; i++)
        {

            names[i] = names[i].Substring(path.ToCharArray().Length, (names[i].ToCharArray().Length - path.ToCharArray().Length));
        }


        return names;
    }


    public static BinaryFormatter GetBinaryFormatter()
    {
        BinaryFormatter formatter = new BinaryFormatter();

        SurrogateSelector selector = new SurrogateSelector();

        Vector3SerializationSurrogate vector3Surrogate = new Vector3SerializationSurrogate();
        QuaternionSerializationSurrogate quaternionSurrogate = new QuaternionSerializationSurrogate();

        selector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3Surrogate);
        selector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quaternionSurrogate);

        formatter.SurrogateSelector = selector;

        return formatter;
    }
}
