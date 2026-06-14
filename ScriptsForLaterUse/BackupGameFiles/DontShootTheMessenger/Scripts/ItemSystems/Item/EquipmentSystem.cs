// EquipmentSystem.cs
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    [Header("Sockets")]
    public Transform primaryWeaponSocket;
    public Transform secondaryWeaponSocket;
    public Transform headSocket;
    public Transform bodySocket;

    GameObject primaryInstance, secondaryInstance, headInstance, bodyInstance;

    public bool Equip(ItemDefinition item)
    {
        if (item.equipSlot == EquipSlot.None || !item.equippedPrefab) return false;
        Transform socket = item.equipSlot switch
        {
            EquipSlot.PrimaryWeapon => primaryWeaponSocket,
            EquipSlot.SecondaryWeapon => secondaryWeaponSocket,
            EquipSlot.Head => headSocket,
            EquipSlot.Body => bodySocket,
            _ => null
        };
        if (!socket) return false;

        // clear existing in that slot
        Unequip(item.equipSlot);

        var inst = Instantiate(item.equippedPrefab, socket);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;

        switch (item.equipSlot)
        {
            case EquipSlot.PrimaryWeapon: primaryInstance = inst; break;
            case EquipSlot.SecondaryWeapon: secondaryInstance = inst; break;
            case EquipSlot.Head: headInstance = inst; break;
            case EquipSlot.Body: bodyInstance = inst; break;
        }
        return true;
    }

    public void Unequip(EquipSlot slot)
    {
        GameObject target = slot switch
        {
            EquipSlot.PrimaryWeapon => primaryInstance,
            EquipSlot.SecondaryWeapon => secondaryInstance,
            EquipSlot.Head => headInstance,
            EquipSlot.Body => bodyInstance,
            _ => null
        };
        if (target) Destroy(target);
        if (slot == EquipSlot.PrimaryWeapon) primaryInstance = null;
        if (slot == EquipSlot.SecondaryWeapon) secondaryInstance = null;
        if (slot == EquipSlot.Head) headInstance = null;
        if (slot == EquipSlot.Body) bodyInstance = null;
    }
}
