using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FoodStockEntry
{
    public FoodStockType stockType;
    public int amount = 0;
    public int capacity = 20;
    public bool infinite;
}

public class FoodStationInventory : MonoBehaviour
{
    public List<FoodStockEntry> stock = new List<FoodStockEntry>();

    public int GetAmount(FoodStockType stockType)
    {
        FoodStockEntry entry = GetEntry(stockType);
        if (entry == null) return 0;
        return entry.infinite ? int.MaxValue : entry.amount;
    }

    public bool HasAtLeast(FoodStockType stockType, int amount)
    {
        FoodStockEntry entry = GetEntry(stockType);
        if (entry == null) return false;
        if (entry.infinite) return true;
        return entry.amount >= amount;
    }

    public bool TryConsume(FoodStockType stockType, int amount)
    {
        if (amount <= 0) return true;

        FoodStockEntry entry = GetEntry(stockType);
        if (entry == null) return false;
        if (entry.infinite) return true;
        if (entry.amount < amount) return false;

        entry.amount -= amount;
        return true;
    }

    public int Add(FoodStockType stockType, int amount)
    {
        if (amount <= 0) return 0;

        FoodStockEntry entry = GetOrCreateEntry(stockType);
        if (entry.infinite) return amount;

        int room = Mathf.Max(0, entry.capacity - entry.amount);
        int added = Mathf.Min(room, amount);
        entry.amount += added;
        return added;
    }

    private FoodStockEntry GetEntry(FoodStockType stockType)
    {
        foreach (FoodStockEntry entry in stock)
        {
            if (entry.stockType == stockType)
                return entry;
        }

        return null;
    }

    private FoodStockEntry GetOrCreateEntry(FoodStockType stockType)
    {
        FoodStockEntry existing = GetEntry(stockType);
        if (existing != null) return existing;

        FoodStockEntry created = new FoodStockEntry
        {
            stockType = stockType,
            amount = 0,
            capacity = 20
        };

        stock.Add(created);
        return created;
    }
}
