using UnityEngine;

[DisallowMultipleComponent]
public class ActivateAnimation : MonoBehaviour
{
    public enum DriveType
    {
        BoolParameter,   // toggles Animator bool (recommended)
        TriggerPair      // fires On trigger / Off trigger depending on state
    }

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private DriveType driveType = DriveType.BoolParameter;

    [Header("State")]
    [Tooltip("Starting state of the switch.")]
    [SerializeField] private bool isOn = false;

    [Header("Bool Parameter (DriveType = BoolParameter)")]
    [SerializeField] private string boolParamName = "IsOn";

    [Header("Trigger Pair (DriveType = TriggerPair)")]
    [SerializeField] private string onTriggerName = "TurnOn";
    [SerializeField] private string offTriggerName = "TurnOff";

    [Header("Optional: Also mirror state to a bool param when using triggers")]
    [SerializeField] private bool mirrorStateBoolWhenUsingTriggers = false;
    [SerializeField] private string mirrorBoolParamName = "IsOn";

    private int _boolHash;
    private int _onTrigHash;
    private int _offTrigHash;
    private int _mirrorBoolHash;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        RebuildHashes();
        ApplyStateToAnimator(); // push initial state
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            RebuildHashes();
            ApplyStateToAnimator();
        }
    }

    private void RebuildHashes()
    {
        _boolHash = Animator.StringToHash(boolParamName ?? "");
        _onTrigHash = Animator.StringToHash(onTriggerName ?? "");
        _offTrigHash = Animator.StringToHash(offTriggerName ?? "");
        _mirrorBoolHash = Animator.StringToHash(mirrorBoolParamName ?? "");
    }

    /// <summary>
    /// Call this from your interact script. It toggles: Off -> On, On -> Off.
    /// </summary>
    public void Activate()
    {
        SetState(!isOn);
    }

    /// <summary>Force a specific state.</summary>
    public void SetState(bool newIsOn)
    {
        isOn = newIsOn;
        ApplyStateToAnimator();
    }

    public bool GetState() => isOn;

    private void ApplyStateToAnimator()
    {
        if (animator == null) return;

        switch (driveType)
        {
            case DriveType.BoolParameter:
                if (!string.IsNullOrWhiteSpace(boolParamName))
                    animator.SetBool(_boolHash, isOn);
                break;

            case DriveType.TriggerPair:
                // Fire the correct trigger based on the NEW state
                if (isOn)
                {
                    if (!string.IsNullOrWhiteSpace(onTriggerName))
                        animator.SetTrigger(_onTrigHash);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(offTriggerName))
                        animator.SetTrigger(_offTrigHash);
                }

                if (mirrorStateBoolWhenUsingTriggers && !string.IsNullOrWhiteSpace(mirrorBoolParamName))
                    animator.SetBool(_mirrorBoolHash, isOn);

                break;
        }
    }
}
