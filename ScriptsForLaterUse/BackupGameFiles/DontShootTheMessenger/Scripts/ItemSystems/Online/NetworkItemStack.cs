// NetworkItemStack.cs
using System;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public struct NetworkItemStack : INetworkSerializable, IEquatable<NetworkItemStack>
{
    public FixedString64Bytes itemId;
    public int quantity;

    public NetworkItemStack(string id, int qty)
    {
        itemId = new FixedString64Bytes(id); // be explicit
        quantity = qty;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> ser) where T : IReaderWriter
    {
        ser.SerializeValue(ref itemId);
        ser.SerializeValue(ref quantity);
    }

    // IEquatable implementation (required by NetworkList)
    public bool Equals(NetworkItemStack other) =>
        itemId.Equals(other.itemId) && quantity == other.quantity;

    public override bool Equals(object obj) =>
        obj is NetworkItemStack other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + itemId.GetHashCode();
            hash = hash * 31 + quantity;
            return hash;
        }
    }

    public static bool operator ==(NetworkItemStack a, NetworkItemStack b) => a.Equals(b);
    public static bool operator !=(NetworkItemStack a, NetworkItemStack b) => !a.Equals(b);
}
