using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CartLine
{
    public ShopItemDefinition item;
    public int quantity = 1;
    public int Subtotal => Mathf.Max(0, (item?.price ?? 0) * Mathf.Max(1, quantity));
}

public class ShopService : MonoBehaviour
{
    [Header("Refs")]
    public ShopCatalog catalog;
    public Wallet wallet;
    public LevelSystem levelSystem;
    public DeliveryManager delivery;
    public RemodelManager remodel;

    [Header("Cart")]
    [SerializeField] private List<CartLine> cart = new();
    public event Action OnCartChanged;
    public IReadOnlyList<CartLine> Cart => cart;

    public bool IsUnlocked(ShopItemDefinition def)
        => def == null || levelSystem == null ? true : levelSystem.Level >= def.levelRequired;

    public void AddToCart(ShopItemDefinition def, int qty = 1)
    {
        if (def == null || qty <= 0) return;
        var existing = cart.FirstOrDefault(c => c.item == def);
        if (existing != null) existing.quantity += qty;
        else cart.Add(new CartLine { item = def, quantity = qty });
        OnCartChanged?.Invoke();
    }

    public void RemoveFromCart(ShopItemDefinition def, int qty = int.MaxValue)
    {
        if (def == null) return;
        var line = cart.FirstOrDefault(c => c.item == def);
        if (line == null) return;
        if (qty >= line.quantity) cart.Remove(line);
        else line.quantity -= qty;
        OnCartChanged?.Invoke();
    }

    public List<CartLine> GetCartItems()
    {
        return cart.Select(c => new CartLine { item = c.item, quantity = c.quantity }).ToList();
    }

    public void ClearCart()
    {
        cart.Clear();
        OnCartChanged?.Invoke();
    }

    public int CartTotal() => cart.Sum(c => c.Subtotal);

    public bool CanCheckout(out string reason)
    {
        reason = "";
        if (wallet == null) { reason = "No wallet"; return false; }
        if (cart.Count == 0) { reason = "Cart is empty"; return false; }
        if (cart.Any(c => !IsUnlocked(c.item))) { reason = "Some items are locked"; return false; }
        var total = CartTotal();
        if (SaveData.Current.mainData.playerData.money < total) { reason = "Not enough funds"; return false; }
        return true;
    }

    public bool Checkout()
    {
        if (!CanCheckout(out var reason))
        {
            //Debug.LogWarning($"Checkout blocked: {reason}");
            return false;
        }

        var total = CartTotal();
        if (!wallet.TrySpend(total)) return false;

        // Fulfill each cart line
        foreach (var line in cart)
        {
                delivery?.DeliverProduct(line.item.productPrefab, line.quantity);
           
        }

        ClearCart();
        return true;
    }
}
