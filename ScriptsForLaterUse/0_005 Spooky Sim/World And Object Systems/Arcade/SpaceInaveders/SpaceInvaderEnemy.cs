using UnityEngine;

public class SpaceInvaderEnemy : MonoBehaviour
{
    [SerializeField]
    private SpaceInvadersGame game;

    public void ClickEnemy()
    {
        //game.HitEnemy();

        transform.localPosition =
            Random.insideUnitCircle * 250f;
    }
}