using UnityEngine;

namespace Remodeling.Runtime
{
    public enum PlayerStatType
    {
        StorageCapacity,
        CustomerCapacity,
        PowerCapacity,
        Reputation
    }

    public class PlayerStats : MonoBehaviour
    {
        public int storageCapacity = 20;
        public int customerCapacity = 5;
        public int powerCapacity = 100;
        public int reputation = 0;

        public int Get(PlayerStatType type) => type switch
        {
            PlayerStatType.StorageCapacity => storageCapacity,
            PlayerStatType.CustomerCapacity => customerCapacity,
            PlayerStatType.PowerCapacity => powerCapacity,
            PlayerStatType.Reputation => reputation,
            _ => 0
        };

        public void Add(PlayerStatType type, int delta)
        {
            Set(type, Get(type) + delta);
        }

        public void Set(PlayerStatType type, int value)
        {
            switch (type)
            {
                case PlayerStatType.StorageCapacity: storageCapacity = value; break;
                case PlayerStatType.CustomerCapacity: customerCapacity = value; break;
                case PlayerStatType.PowerCapacity: powerCapacity = value; break;
                case PlayerStatType.Reputation: reputation = value; break;
            }
        }
    }
}
