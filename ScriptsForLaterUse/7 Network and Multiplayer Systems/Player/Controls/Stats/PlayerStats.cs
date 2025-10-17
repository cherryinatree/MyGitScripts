using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaRegenPerSec = 15f;
    [SerializeField] float staminaSprintCostPerSec = 25f;

    public NetworkVariable<float> Health = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Stamina = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Shield = new(writePerm: NetworkVariableWritePermission.Server);

    bool _isSprinting; // set via server from movement if you want server-truth sprint cost

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Health.Value = maxHealth;
            Stamina.Value = maxStamina;
            Shield.Value = 0f;
        }
    }

    void Update()
    {
        if (!IsServer) return;
        float dt = Time.deltaTime;

        // simple stamina model
        float regen = staminaRegenPerSec * dt;
        float cost = _isSprinting ? staminaSprintCostPerSec * dt : 0f;

        Stamina.Value = Mathf.Clamp(Stamina.Value + regen - cost, 0f, maxStamina);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(float amount)
    {
        float remaining = amount;

        if (Shield.Value > 0f)
        {
            float absorb = Mathf.Min(Shield.Value, remaining);
            Shield.Value -= absorb;
            remaining -= absorb;
        }

        if (remaining > 0f)
        {
            Health.Value = Mathf.Max(0f, Health.Value - remaining);
            if (Health.Value <= 0f)
            {
                // TODO: death/respawn
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSprintingServerRpc(bool sprinting)
    {
        _isSprinting = sprinting;
    }
}
