using MoreMountains.CorgiEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerOverhead : MonoBehaviour
{
    JoinLocations joinLocations;
    private int myLocation = 0;
    private int SelectedCharacter = 0;
    private GameObject myCharacter;
    public ClassList classList;

    private PlayerInput playerInput;
    public bool isReady = false;

    // Start is called before the first frame update
    void Start()
    {
        joinLocations = GameObject.Find("JoinManager").GetComponent<JoinLocations>();
        joinLocations.AddPlayer(this);
        myLocation = joinLocations.currentLocation;
        joinLocations.currentLocation++;
        playerInput = GetComponent<PlayerInput>();
        SelectMyCharacter();
    }


    // Update is called once per frame
    void Update()
    {
        if (playerInput.actions["Accept"].triggered)
        {
            SelectedCharacter++;
            if (SelectedCharacter >= classList.classes.Count)
            {
                SelectedCharacter = 0;
            }
            DestroyImmediate(myCharacter);
            SelectMyCharacter();
        }
        if (playerInput.actions["Jump"].triggered)
        {
            isReady = !isReady;

        }
    }
    private void SelectMyCharacter()
    {
        GameObject myCharacterPrefab = Resources.Load<GameObject>("Prefabs/Characters/"+ classList.classes[SelectedCharacter].Class.prefabLocation);

        if (myCharacterPrefab != null) {
            myCharacter = Instantiate(myCharacterPrefab, joinLocations.locations[myLocation].transform.position, Quaternion.identity);
            myCharacter.transform.SetParent(joinLocations.locations[myLocation].transform);
            myCharacter.GetComponent<Character>().PlayerID = "Player" + myLocation;
        }
    }
}
