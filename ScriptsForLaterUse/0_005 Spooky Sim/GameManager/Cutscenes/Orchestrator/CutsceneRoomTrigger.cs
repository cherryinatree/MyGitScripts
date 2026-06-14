using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Cherry/Cutscenes/Cutscene Room Trigger")]
[RequireComponent(typeof(Collider))]
public class CutsceneRoomTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

    [Header("Cutscene")]
    [SerializeField] private GeneralCutsceneOrchestrator cutscene;

    [Header("Activate After Cutscene")]
    [Tooltip("These behaviours will be disabled on Awake, then enabled when the cutscene finishes.")]
    [SerializeField] private Behaviour[] enableAfterCutscene;

    [Tooltip("These GameObjects will be disabled on Awake, then enabled when the cutscene finishes.")]
    [SerializeField] private GameObject[] activateAfterCutscene;

    [Header("Events")]
    public UnityEvent onPlayerEntered;
    public UnityEvent onCutsceneFinished;

    private bool _triggered;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        SetBehaviours(enableAfterCutscene, false);
        SetObjects(activateAfterCutscene, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;
        onPlayerEntered?.Invoke();

        if (cutscene != null)
        {
            cutscene.Finished -= HandleCutsceneFinished;
            cutscene.Finished += HandleCutsceneFinished;
            cutscene.Play();
        }
        else
        {
            HandleCutsceneFinished();
        }

        if (triggerOnce)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private void HandleCutsceneFinished()
    {
        if (cutscene != null)
        {
            cutscene.Finished -= HandleCutsceneFinished;
        }

        SetBehaviours(enableAfterCutscene, true);
        SetObjects(activateAfterCutscene, true);

        onCutsceneFinished?.Invoke();
    }

    private void SetBehaviours(Behaviour[] behaviours, bool enabledState)
    {
        if (behaviours == null) return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
            {
                behaviours[i].enabled = enabledState;
            }
        }
    }

    private void SetObjects(GameObject[] objects, bool activeState)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(activeState);
            }
        }
    }
}