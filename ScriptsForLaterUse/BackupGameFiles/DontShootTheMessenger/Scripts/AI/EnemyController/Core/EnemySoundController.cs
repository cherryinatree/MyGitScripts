using UnityEngine;

public class EnemySoundController : EnemyAction
{
    public AudioClip stepSound;
    public AudioClip AttackSound;
    public AudioClip IdleGrowl;

    public void PlayStep()
    {
        if (core.audioSource != null)
            core.audioSource.PlayOneShot(stepSound);
    }

    public void PlayAttack()
    {
        if (core.audioSource != null)
            core.audioSource.PlayOneShot(AttackSound);
    }

    public void PlayIdle()
    {
        if (core.audioSource != null)
            core.audioSource.PlayOneShot(IdleGrowl);
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (core.audioSource != null)
            core.audioSource.PlayOneShot(clip);
    }
}
