using UnityEngine;

[System.Serializable]
public class CombatTranistion
{

    /// this transition's decision
    public CombatDecision Decision;
    /// the state to transition to if this Decision returns true
    public string TrueState;
    /// the state to transition to if this Decision returns false
    public string FalseState;
}
