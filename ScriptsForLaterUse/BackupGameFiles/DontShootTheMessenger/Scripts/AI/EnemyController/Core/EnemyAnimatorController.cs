using UnityEngine;

public class EnemyAnimatorController : EnemyAction
{
    public void PlayWalk(bool walking)
    {
        if (core.animator != null)
            core.animator.SetBool("isWalking", walking);
    }

    public void PlayAttack()
    {
        if (core.animator != null)
            core.animator.SetTrigger("attack");
    }

    public void PlayIdle()
    {
        if (core.animator != null)
            core.animator.SetBool("isWalking", false);
    }

    public void PlayTrigger(string triggerName)
    {
        if (core.animator != null)
            core.animator.SetTrigger(triggerName);
    }
}
