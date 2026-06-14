using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class CoreEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 3f;
    public float turnSpeed = 5f;
    public float jumpForce = 5f;
    public int health = 100;

    [Header("Perception")]
    public MonsterPerception perception;

    [Header("Components")]
    public Animator animator;
    public AudioSource audioSource;

    [HideInInspector] public Collider col;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Vector3 spawnPoint;

    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // keeps it upright
        spawnPoint = transform.position;

        if (perception == null) perception = GetComponent<MonsterPerception>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
}
