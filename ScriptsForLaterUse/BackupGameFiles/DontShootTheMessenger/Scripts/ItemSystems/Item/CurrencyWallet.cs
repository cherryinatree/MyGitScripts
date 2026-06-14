// CurrencyWallet.cs
using UnityEngine;

public class CurrencyWallet : MonoBehaviour
{
    public int credits = 0;
    public System.Action OnChanged;

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (credits < amount) return false;
        credits -= amount; OnChanged?.Invoke(); return true;
    }

    public void Add(int amount)
    {
        credits += Mathf.Max(0, amount);
        OnChanged?.Invoke();
    }
}
