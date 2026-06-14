using System.Collections;
using UnityEngine;

public interface IRobotInteractable
{
    Transform InteractionPoint { get; }
    bool CanInteract(GameObject robot);
    IEnumerator Interact(RobotNavigator robot);
}
