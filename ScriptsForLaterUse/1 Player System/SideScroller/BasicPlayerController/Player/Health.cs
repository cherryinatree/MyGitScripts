using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameObject))]
public class Health : MonoBehaviour
{

    public GameObject defensiveBubble0;
    public GameObject defensiveBubble1;

    public float health = 1;

    public float flashTime;
    Color[] origionalColors;
    private SkinnedMeshRenderer[] SkinnedRenderer;
    private MeshRenderer[] renderer;
    private bool isSkinned = false;
    private float hurtTimer;
    public float hurtTime = 2;
    private Rigidbody rb;
    public float forceMutiply = 25;
    private Player player;

    void Start()
    {
        hurtTimer = 0;
        defensiveBubble0.SetActive(false);
        defensiveBubble1.SetActive(false);
        rb = GetComponent<Rigidbody>();
        player = GetComponent<Player>();

        if (GetComponentInChildren<SkinnedMeshRenderer>())
        {
            SkinnedRenderer = GetComponentsInChildren<SkinnedMeshRenderer>();
            isSkinned = true;
            origionalColors = new Color[SkinnedRenderer.Length];
            for (int i = 0; i < SkinnedRenderer.Length; i++)
            {

                origionalColors[i] = SkinnedRenderer[i].material.color;
            }
        }
        else
        {
            renderer = GetComponentsInChildren<MeshRenderer>();
            isSkinned = false;

            origionalColors = new Color[renderer.Length];
            for (int i = 0; i < renderer.Length; i++)
            {

                origionalColors[i] = renderer[i].material.color;
            }
        }
    }
    private void FixedUpdate()
    {
        hurtTimer += Time.deltaTime;
    }


    public void Defense()
    {
        if (health >= 2)
        {
            health++;
            defensiveBubble1.SetActive(true);
        }
        if (health == 1)
        {
            health = 2;
            defensiveBubble0.SetActive(true);
        }
        
    }

    public void Damaged(bool Knockback)
    {
        if (hurtTimer > hurtTime)
        {
            if (health > 1)
            {
                health -= 1;
                FlashRed();
                if (health == 1)
                {

                    defensiveBubble0.SetActive(false);
                    defensiveBubble1.SetActive(false);
                }
                if (health == 2)
                {

                    defensiveBubble0.SetActive(true);
                    defensiveBubble1.SetActive(false);
                }
            }
            else
            {
                health = 0;
                Destroy(gameObject);
            }
            hurtTimer = 0;
            if (player._isGrounded && Knockback)
            {
                rb.AddForce(-gameObject.transform.forward * forceMutiply);
            }
        }
    }

    void FlashRed()
    {
        if (isSkinned)
        {
            for (int i = 0; i < SkinnedRenderer.Length; i++)
            {

                SkinnedRenderer[i].material.color = Color.red;
            }

        }
        else
        {

            for (int i = 0; i < renderer.Length; i++)
            {

                renderer[i].material.color = Color.red;
            }
        }
        Invoke("ResetColor", flashTime);
    }
    void ResetColor()
    {
        if (isSkinned)
        {

            for (int i = 0; i < SkinnedRenderer.Length; i++)
            {

                SkinnedRenderer[i].material.color = origionalColors[i];
            }
        }
        else
        {

            for (int i = 0; i < renderer.Length; i++)
            {

                renderer[i].material.color = origionalColors[i];
            }
        }
    }

}
