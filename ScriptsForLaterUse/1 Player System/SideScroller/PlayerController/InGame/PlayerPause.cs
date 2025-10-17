using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerPause : CharacterAbility
{
    public GameObject PauseMenu;


    protected override void Initialization()
    {
        base.Initialization();
        PauseMenu.SetActive(false);
    }

    protected override void HandleInput()
    {

        if (_inputManager.PauseButton.State.CurrentState == MMInput.ButtonStates.ButtonUp)
        {
            TriggerPause();
        }
    }

    private void TriggerPause() 
    { 
        PauseMenu.SetActive(!PauseMenu.activeSelf);
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
