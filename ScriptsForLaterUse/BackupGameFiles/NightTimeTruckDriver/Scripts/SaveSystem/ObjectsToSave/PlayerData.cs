using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    [Header("Truck Stats")]
    public float gas = 100f; // percentage
    public float hunger = 100f; // percentage
    public float sanity = 100f; // percentage
    public float speed = 0f; // mph
    public float rpm = 0f;
    public int gear = 1;

    [Header("Delivery Stats")]
    public float distanceTraveled = 0f; // miles
    public float distanceToObjective = 50f; // miles
    public float payPerMile = 2.5f; // dollars per mile
    public float bonusPerMile = 1.0f; // dollars per mile
    public float money = 0f;

    [Header("Vehicle References")]

    private Vector3 lastPosition;
    public int day = 1;

    public string Scene = "main.save"; // Default scene name, can be changed

    public string truckPath = "Prefabs/Truck/Truck";
    public string playerPath = "Prefabs/Player/PlayerModel1";
}
