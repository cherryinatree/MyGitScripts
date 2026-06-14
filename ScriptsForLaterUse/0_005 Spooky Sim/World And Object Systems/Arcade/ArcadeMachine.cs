using UnityEngine;

public class ArcadeMachine : MonoBehaviour
{
    [SerializeField] private ArcadeGameType gameType;

    public void Interact()
    {
        ArcadeUIManager.Instance.OpenGame(gameType);
    }
}

public enum ArcadeGameType
{
    SpaceInvaders,
    WhackAMole
}