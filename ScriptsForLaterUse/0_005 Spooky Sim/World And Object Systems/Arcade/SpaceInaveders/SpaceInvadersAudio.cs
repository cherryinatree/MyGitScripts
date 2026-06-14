using UnityEngine;

public class SpaceInvadersAudio : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX")]
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip enemyKilledClip;
    [SerializeField] private AudioClip playerHitClip;
    [SerializeField] private AudioClip powerUpClip;
    [SerializeField] private AudioClip levelCompleteClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip enemyStepClip;
    [SerializeField] private AudioClip ticketClip;

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null)
            return;

        if (clip == null)
        {
            musicSource.Stop();
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void Shoot() => PlaySfx(shootClip);
    public void EnemyKilled() => PlaySfx(enemyKilledClip);
    public void PlayerHit() => PlaySfx(playerHitClip);
    public void PowerUp() => PlaySfx(powerUpClip);
    public void LevelComplete() => PlaySfx(levelCompleteClip);
    public void GameOver() => PlaySfx(gameOverClip);
    public void EnemyStep() => PlaySfx(enemyStepClip);
    public void Tickets() => PlaySfx(ticketClip);

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}