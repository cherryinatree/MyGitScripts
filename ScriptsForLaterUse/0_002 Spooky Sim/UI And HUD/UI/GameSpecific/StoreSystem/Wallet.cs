using UnityEngine;
using System;

public class Wallet : MonoBehaviour
{
    [SerializeField, Min(0)] private int balance = 500;
    public event Action<int> OnBalanceChanged;

    public int Balance => balance;

    public bool TrySpend(int amount)
    {
        balance = SaveData.Current.mainData.playerData.money;
        if (amount < 0) return false;
        if (balance < amount) return false;
        balance -= amount;
        SaveData.Current.mainData.playerData.money -= amount;
        OnBalanceChanged?.Invoke(balance);
        return true;
    }

    public void Add(int amount)
    {
        balance += Mathf.Max(0, amount);
        SaveData.Current.mainData.playerData.money += Mathf.Max(0, amount);
        OnBalanceChanged?.Invoke(balance);
    }
}
