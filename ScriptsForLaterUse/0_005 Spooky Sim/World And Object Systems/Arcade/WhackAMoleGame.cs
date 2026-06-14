using TMPro;
using UnityEngine;

public class WhackAMoleGame : ArcadeGame
{
    [SerializeField] private TMP_Text scoreText;

    private float timer;

    public override void StartGame()
    {
        base.StartGame();

        timer = 30f;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            EndGame();
        }
    }

    public void Whack()
    {
        score += 5;

        scoreText.text = score.ToString();
    }
}