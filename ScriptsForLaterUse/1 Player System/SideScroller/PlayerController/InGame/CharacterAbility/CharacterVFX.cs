using MoreMountains.CorgiEngine;
using UnityEngine;
using UnityEngine.VFX;

public class CharacterVFX : MonoBehaviour
{
    
    private Timer attackTimer;
    public float delay = 0.5f;

    private void Start()
    {
        attackTimer = new Timer(delay);
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(attackTimer.ClockTick())
        {
            attackTimer.RestartTimer();
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if(attackTimer != null)
        {
            attackTimer.RestartTimer();
        }
    }

}
