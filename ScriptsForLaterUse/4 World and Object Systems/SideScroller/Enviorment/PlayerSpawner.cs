using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        CharacterSelect.SetCharacters();
        string name = CharacterSelect.CharacterSelects[GameSingleton.Instance.save.CharacterSelected];
        
        GameObject prefab = Resources.Load("Prefabs/Created/PCs/"+ name) as GameObject;
        prefab.GetComponent<CharacterSkinController>().materials = GameSingleton.Instance.save.DigiSkinSelected;

        Instantiate(prefab, gameObject.transform.position, gameObject.transform.rotation);
    }

    public void SpawnPlayer()
    {

        CharacterSelect.SetCharacters();
        string name = CharacterSelect.CharacterSelects[GameSingleton.Instance.save.CharacterSelected];

        GameObject prefab = Resources.Load("Prefabs/Created/PCs/" + name) as GameObject;

        Instantiate(prefab, gameObject.transform.position, gameObject.transform.rotation);
    }
}
