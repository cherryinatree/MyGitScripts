using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    public PlayerInput playerInput;

    private GameObject player;
    public ClassList classList;
    public PlayerData Data;

    #region Player Movement Variables
    //Set all of these up in the inspector
    [Header("Checks")]
    [SerializeField] private Transform _groundCheckPoint;
    //Size of groundCheck depends on the size of your character generally you want them slightly small than width (for ground) and height (for the wall check)
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);

    [Header("Layers & Tags")]
    [SerializeField] private LayerMask _groundLayer;

    private float gravityScale;

    private PlayerMovement playerMovement;
    #endregion

    PlayerAbilities playerAbilities;

    private void Start()
    {
        GameObject player =  GameObject.Find("Dog");
        playerMovement = new PlayerMovement(player.transform, playerInput, Data, classList, _groundLayer, 
            _groundCheckSize, _wallCheckSize, _groundCheckPoint, _frontWallCheckPoint, _backWallCheckPoint);
        playerAbilities = new PlayerAbilities(playerInput, classList, player.transform.Find("FirePoint"), player.transform, player.transform.Find("MeleePoint"));
    }

    private void Update()
    {
        playerMovement.UpdateMovement();
        playerAbilities.UpdateAbilities();
    }

    private void FixedUpdate()
    {
        playerMovement.FixedUpdateMovement();
    }

}
