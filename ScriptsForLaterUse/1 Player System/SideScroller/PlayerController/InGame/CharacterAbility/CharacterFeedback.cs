using MoreMountains.CorgiEngine;
using UnityEngine;

public class CharacterFeedback : CharacterAbility
{
    public GameObject ImmuneVFX;
    public GameObject JumpVFX;

    protected override void Initialization()
    {
        base.Initialization();
    }

    // Update is called once per frame
    void Update()
    {
        Jump();
        ImmuneRender();
    }

    private void Jump()
    {

    }

    private void ImmuneRender()
    {
        if (_character.CharacterHealth.ImmuneToDamage)
        {
            ImmuneVFX.SetActive(true);
        }
        else
        {
            ImmuneVFX.SetActive(false);
        }
    }
}
