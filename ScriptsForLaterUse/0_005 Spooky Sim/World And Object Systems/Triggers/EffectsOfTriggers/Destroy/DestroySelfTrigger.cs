using UnityEngine;

public class DestroySelfTrigger : MonoBehaviour
{
    public void DestroyMySelf()
    {
        Destroy(this.gameObject);
    }
}
