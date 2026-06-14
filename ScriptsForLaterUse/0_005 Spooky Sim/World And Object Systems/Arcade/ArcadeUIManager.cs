using UnityEngine;

public class ArcadeUIManager : MonoBehaviour
{
    public static ArcadeUIManager Instance;

    [SerializeField] private GameObject arcadeCanvas;

    [SerializeField] private SpaceInvadersGame spaceInvaders;
    [SerializeField] private WhackAMoleGame whackAMole;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenGame(ArcadeGameType type)
    {
        arcadeCanvas.SetActive(true);

        switch (type)
        {
            case ArcadeGameType.SpaceInvaders:
                spaceInvaders.BeginGame();
                break;

            case ArcadeGameType.WhackAMole:
                whackAMole.StartGame();
                break;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseGame()
    {
        arcadeCanvas.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}