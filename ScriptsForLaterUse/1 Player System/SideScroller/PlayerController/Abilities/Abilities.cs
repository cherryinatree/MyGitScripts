using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Abilities", menuName = "Abilities/Abilities")]
public class Abilities : ScriptableObject
{
    public enum AbilityHitBox { Circle, Box, SpawnPrefab}

    [Header("Primary Information")]
    public int abilityID;
    public string abilityName;
    public string abilityDescription;
    public string abilityType;
    public string abilityLocation;
    public List<InputBuffer.InputBufferButtons> comboButtons;

    [Space(10), Header("VFX Information")]
    public string abilityAnimationTrigger;
    public GameObject abilitySelfVFX;
    public GameObject abilityImpactVFX;


    [Space(10), Header("Combat Information")]
    public int abilityLevel;
    public int abilityDamage;
    public float abilityCooldown;
    public bool damage;
    public Vector2 knockBackForce;

    [Space(10), Header("Hit Box Settings")]
    [Range(0f, 1f)]
    public float damageTime;
    [Range(0f, 1f)]
    public float recoveryTime;
    public AbilityHitBox hitBoxType;
    public Vector2 hitBoxPosition;
    public Vector2 hitBoxSize;
    public float hitBoxAngle;
    public float hitBoxRadius;



    [Space(10), Header("Spawn Prefab Settings")]
    public GameObject abilityPrefab;
    public bool joyDirection = false;
    public float speed = 15;
    public Vector3 facingDirection = Vector3.zero;
    public Vector3 turnObject = Vector3.zero;
    public Vector3 spawnPoint;


    [Space(10), Header("Combo Settings")]
    public Abilities comboAbility;

}
