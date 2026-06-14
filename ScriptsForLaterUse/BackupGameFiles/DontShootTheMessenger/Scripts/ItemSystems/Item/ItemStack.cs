// ItemStack.cs
[System.Serializable]
public class ItemStack
{
    public ItemDefinition item;
    public int quantity;

    public ItemStack(ItemDefinition item, int qty)
    {
        this.item = item;
        this.quantity = qty;
    }

    public int SpaceLeft => item.stackable ? (item.maxStack - quantity) : 0;
}
