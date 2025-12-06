// MissionFlagSetter.cs
using UnityEngine;

[AddComponentMenu("Cherry/Conditions/Mission Flag Setter")]
public class MissionFlagSetter : MonoBehaviour
{
    [SerializeField] private BoolFlagSO flag;

    public void SetTrue() { if (flag) flag.SetTrue(); }
    public void SetFalse() { if (flag) flag.SetFalse(); }
    public void Set(bool v) { if (flag) flag.Set(v); }
}
