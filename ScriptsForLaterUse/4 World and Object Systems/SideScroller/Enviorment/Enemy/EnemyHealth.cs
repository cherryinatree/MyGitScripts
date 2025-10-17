using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 1;

    public float flashTime;
    Color origionalColor;
    private SkinnedMeshRenderer SkinnedRenderer;
    private MeshRenderer renderer;
    private bool isSkinned = false;

    void Start()
    {
        if (GetComponentInChildren<SkinnedMeshRenderer>())
        {
            SkinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            isSkinned = true;

            origionalColor = SkinnedRenderer.material.color;
        }
        else
        {

            renderer = GetComponent<MeshRenderer>();
            isSkinned = false;

            origionalColor = renderer.material.color;
        }
    }
    void FlashRed()
    {
        if (isSkinned)
        {

            SkinnedRenderer.material.color = Color.red;
        }
        else
        {

            renderer.material.color = Color.red;
        }
        Invoke("ResetColor", flashTime);
    }
    void ResetColor()
    {
        if (isSkinned)
        {

            SkinnedRenderer.material.color = origionalColor;
        }
        else
        {

            renderer.material.color = origionalColor;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.transform.tag == "PCweapon")
        {
            float damage = collision.transform.GetComponent<Shooting>().Damage;
            health -= damage;
            if(health <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                FlashRed();
            }
        }
        if (collision.gameObject.transform.tag == "MOB")
        {
            if (GetComponent<Sentry>())
            {
                GetComponent<Sentry>().NextPoint();
            }
        }
    }
}
