using UnityEngine;

public class EnemyJump : EnemyAction
{
    public void Jump()
    {
        if (IsGrounded())
        {
            core.rb.AddForce(Vector3.up * core.jumpForce, ForceMode.Impulse);

            if (core.animator != null)
                core.animator.SetTrigger("jump");

            if (core.audioSource != null)
                core.audioSource.PlayOneShot(Resources.Load<AudioClip>("JumpSound"));
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(core.transform.position, Vector3.down, core.col.bounds.extents.y + 0.1f);
    }
}
