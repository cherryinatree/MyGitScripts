using MoreMountains.Tools;
using UnityEngine;

public class CD_BufferAbilityChosen : CombatDecision
{
    private InputBuffer attackBuffer;

    public override bool Decide()
    {
        return attackBuffer.hasAbilityBeenChosen;
    }

    protected override void Awake()
    {
        base.Awake();
        attackBuffer = GetComponent<InputBuffer>();
    }
}
