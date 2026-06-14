using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//[AddComponentMenu("Cherry/Anomalies/Cinemachine Scare Sequence Controller")]
public class CinemachineScareSequenceController : MonoBehaviour
{
    public enum TriggerMode
    {
        ExternalOnly,
        ProximityOnly,
        Both
    }

    public enum ReentryMode
    {
        OnlyOnce,
        AllowAfterFinished
    }

    [Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [Serializable]
    public class ScareBeat
    {
        [Header("Label")]
        public string beatName = "Scare Beat";

        [Header("Cinemachine Camera")]
        [Tooltip("The Cinemachine camera GameObject for this beat.")]
        public GameObject cinemachineCameraObject;

        [Tooltip("How long to stay on this camera before moving to the next beat.")]
        [Min(0f)] public float holdDuration = 1f;

        [Header("Monster Animation")]
        [Tooltip("Optional Animator trigger fired at the start of this beat.")]
        public string monsterAnimatorTrigger;

        [Header("Audio")]
        public AudioClip audioClip;

        [Tooltip("If true, this beat waits for the audio clip to finish before continuing.")]
        public bool waitForAudioToFinish = false;

        [Header("Optional Damage")]
        public bool applyDamageOnBeatStart = false;
        [Min(0f)] public float damageAmount = 10f;

        [Header("Events")]
        public UnityEvent onBeatStart;
        public UnityEvent onBeatEnd;
    }

    private struct BehaviourState
    {
        public Behaviour behaviour;
        public bool wasEnabled;
    }

    private struct GameObjectState
    {
        public GameObject gameObject;
        public bool wasActive;
    }

    [Header("Trigger")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.Both;
    [SerializeField] private ReentryMode reentryMode = ReentryMode.OnlyOnce;
    [SerializeField] private string playerTag = "Player";
    private Vector3 originalPlayerPosition;

    [Tooltip("Optional. If assigned, this object is treated as the player.")]
    [SerializeField] private GameObject playerOverride;

    [Header("Cinemachine Cameras")]
    [Tooltip("Your normal gameplay Cinemachine camera GameObject.")]
    [SerializeField] private GameObject normalGameplayCameraObject;

    [Header("Player Reposition After Sequence")]
    [SerializeField] private bool movePlayerXZToLastScareCameraOnComplete = true;

    [Tooltip("Optional. If assigned, this transform is moved instead of the detected player object. Use this for your actual player root.")]
    [SerializeField] private Transform playerRootToMoveOverride;

    [Tooltip("Keeps the player's Y position unchanged so they do not get moved to the thrown camera's floor/head height.")]
    [SerializeField] private bool preservePlayerY = true;

    [Tooltip("Optional offset added after copying the final scare camera X/Z. Useful if the camera is slightly inside the monster or wall.")]
    [SerializeField] private Vector3 finalPlayerPositionOffset = Vector3.zero;

    [Tooltip("Scare cameras played in order.")]
    [SerializeField] private List<ScareBeat> scareBeats = new List<ScareBeat>();

    [Tooltip("If true, scare cameras are disabled when the sequence starts, then enabled one at a time.")]
    [SerializeField] private bool disableAllScareCamerasOnStart = true;

    [Header("Monster")]
    [SerializeField] private Animator monsterAnimator;

    [Tooltip("Optional trigger fired immediately when the full sequence starts.")]
    [SerializeField] private string monsterStartTrigger = "Rush";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Player Control Lock")]
    [Tooltip("Movement, mouse look, interaction, ray/weapon scripts, etc.")]
    [SerializeField] private List<Behaviour> behavioursToDisableDuringSequence = new List<Behaviour>();

    [Tooltip("Optional objects to hide during the sequence, such as UI prompts or held tools.")]
    [SerializeField] private List<GameObject> objectsToDisableDuringSequence = new List<GameObject>();

    [Tooltip("Optional objects to enable during the sequence, such as monster props, fake arms, scare lighting, VFX, blockers, etc.")]
    [SerializeField] private List<GameObject> objectsToEnableDuringSequence = new List<GameObject>();

    [Tooltip("Optional. Freezes the player's Rigidbody during the scare.")]
    [SerializeField] private bool freezePlayerRigidbody = false;

    [Header("Damage")]
    [Tooltip("Optional object that receives damage. If empty, the detected player is used.")]
    [SerializeField] private GameObject damageReceiverOverride;

    [Tooltip("The method called by SendMessage. Examples: TakeDamage, Damage, ApplyDamage.")]
    [SerializeField] private string damageMethodName = "TakeDamage";

    public FloatEvent onDamage;

    [Header("Sequence Events")]
    public UnityEvent onSequenceStart;
    public UnityEvent onSequenceComplete;

    [Header("Advanced")]
    [SerializeField] private bool useUnscaledTime = false;

    private bool hasPlayed;
    private bool isPlaying;

    private readonly List<BehaviourState> disabledBehaviourStates = new();
    private readonly List<GameObjectState> disabledObjectStates = new();
    private readonly List<GameObjectState> enabledObjectStates = new();

    private Rigidbody activePlayerRigidbody;
    private RigidbodyConstraints originalRigidbodyConstraints;

    private Coroutine activeRoutine;
    private GameObject activePlayer;
    private GameObject activeDamageReceiver;


    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        monsterAnimator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (disableAllScareCamerasOnStart)
            SetAllScareCamerasActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerMode == TriggerMode.ExternalOnly)
            return;

        if (isPlaying)
            return;

        if (hasPlayed && reentryMode == ReentryMode.OnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        Play(other.gameObject);
    }

    public void Play()
    {
        Play(null);
    }

    public void Play(GameObject player)
    {
        if (triggerMode == TriggerMode.ProximityOnly && player == null)
            return;

        if (isPlaying)
            return;

        if (hasPlayed && reentryMode == ReentryMode.OnlyOnce)
            return;

        activeRoutine = StartCoroutine(SequenceRoutine(player));
    }

    public void ResetSequence()
    {
        hasPlayed = false;
    }

    public void StopSequenceAndRestore()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = null;
        isPlaying = false;

        RestorePlayerControl();
        RestoreGameplayCamera();
    }

    private IEnumerator SequenceRoutine(GameObject playerFromTrigger)
    {
        isPlaying = true;
        hasPlayed = true;

        ResolveReferences(playerFromTrigger);

        LockPlayerControl();

        onSequenceStart?.Invoke();

        if (monsterAnimator != null && !string.IsNullOrWhiteSpace(monsterStartTrigger))
            monsterAnimator.SetTrigger(monsterStartTrigger);

        if (normalGameplayCameraObject != null)
            normalGameplayCameraObject.SetActive(false);

        SetAllScareCamerasActive(false);

        for (int i = 0; i < scareBeats.Count; i++)
        {
            ScareBeat beat = scareBeats[i];

            if (beat == null)
                continue;

            yield return RunBeat(beat);
        }

        if (movePlayerXZToLastScareCameraOnComplete)
            MovePlayerXZToLastScareCamera();

        Physics.SyncTransforms();

        onSequenceComplete?.Invoke();

        RestoreGameplayCamera();

        // This MUST happen last.
        // Your movement / character controller scripts come back only after teleporting.
        RestorePlayerControl();

        isPlaying = false;
        activeRoutine = null;
    }

    private IEnumerator RunBeat(ScareBeat beat)
    {
        beat.onBeatStart?.Invoke();

        SetAllScareCamerasActive(false);

        if (beat.cinemachineCameraObject != null)
            beat.cinemachineCameraObject.SetActive(true);

        if (monsterAnimator != null && !string.IsNullOrWhiteSpace(beat.monsterAnimatorTrigger))
            monsterAnimator.SetTrigger(beat.monsterAnimatorTrigger);

        if (beat.applyDamageOnBeatStart)
            ApplyDamage(beat.damageAmount);

        if (audioSource != null && beat.audioClip != null)
        {
            audioSource.clip = beat.audioClip;
            audioSource.Play();
        }

        float elapsed = 0f;

        while (elapsed < beat.holdDuration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }

        if (beat.waitForAudioToFinish && audioSource != null)
        {
            while (audioSource.isPlaying)
                yield return null;
        }

        beat.onBeatEnd?.Invoke();
    }

    private void ResolveReferences(GameObject playerFromTrigger)
    {
        activePlayer = playerOverride != null ? playerOverride : playerFromTrigger;

        if (activePlayer == null && !string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (foundPlayer != null)
                activePlayer = foundPlayer;
        }

        activeDamageReceiver = damageReceiverOverride != null ? damageReceiverOverride : activePlayer;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (freezePlayerRigidbody && activePlayer != null)
            activePlayerRigidbody = activePlayer.GetComponentInParent<Rigidbody>();

        Transform playerRoot = GetPlayerRootToMove();

        if (playerRoot != null)
            originalPlayerPosition = playerRoot.position;
    }

    private void RestoreGameplayCamera()
    {
        SetAllScareCamerasActive(false);

        if (normalGameplayCameraObject != null)
            normalGameplayCameraObject.SetActive(true);
    }

    private void SetAllScareCamerasActive(bool active)
    {
        for (int i = 0; i < scareBeats.Count; i++)
        {
            if (scareBeats[i] == null)
                continue;

            GameObject camObj = scareBeats[i].cinemachineCameraObject;

            if (camObj != null)
                camObj.SetActive(active);
        }
    }

    private void LockPlayerControl()
    {
        disabledBehaviourStates.Clear();
        disabledObjectStates.Clear();
        enabledObjectStates.Clear();

        for (int i = 0; i < behavioursToDisableDuringSequence.Count; i++)
        {
            Behaviour behaviour = behavioursToDisableDuringSequence[i];

            if (behaviour == null)
                continue;

            disabledBehaviourStates.Add(new BehaviourState
            {
                behaviour = behaviour,
                wasEnabled = behaviour.enabled
            });

            behaviour.enabled = false;
        }

        for (int i = 0; i < objectsToDisableDuringSequence.Count; i++)
        {
            GameObject obj = objectsToDisableDuringSequence[i];

            if (obj == null)
                continue;

            disabledObjectStates.Add(new GameObjectState
            {
                gameObject = obj,
                wasActive = obj.activeSelf
            });

            obj.SetActive(false);
        }

        for (int i = 0; i < objectsToEnableDuringSequence.Count; i++)
        {
            GameObject obj = objectsToEnableDuringSequence[i];

            if (obj == null)
                continue;

            enabledObjectStates.Add(new GameObjectState
            {
                gameObject = obj,
                wasActive = obj.activeSelf
            });

            obj.SetActive(true);
        }

        if (freezePlayerRigidbody && activePlayerRigidbody != null)
        {
            originalRigidbodyConstraints = activePlayerRigidbody.constraints;

#if UNITY_6000_0_OR_NEWER
            activePlayerRigidbody.linearVelocity = Vector3.zero;
#else
        activePlayerRigidbody.velocity = Vector3.zero;
#endif

            activePlayerRigidbody.angularVelocity = Vector3.zero;
            activePlayerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void RestorePlayerControl()
    {
        for (int i = 0; i < disabledBehaviourStates.Count; i++)
        {
            BehaviourState state = disabledBehaviourStates[i];

            if (state.behaviour != null)
                state.behaviour.enabled = state.wasEnabled;
        }

        for (int i = 0; i < disabledObjectStates.Count; i++)
        {
            GameObjectState state = disabledObjectStates[i];

            if (state.gameObject != null)
                state.gameObject.SetActive(state.wasActive);
        }

        for (int i = 0; i < enabledObjectStates.Count; i++)
        {
            GameObjectState state = enabledObjectStates[i];

            if (state.gameObject != null)
                state.gameObject.SetActive(state.wasActive);
        }

        if (freezePlayerRigidbody && activePlayerRigidbody != null)
            activePlayerRigidbody.constraints = originalRigidbodyConstraints;

        disabledBehaviourStates.Clear();
        disabledObjectStates.Clear();
        enabledObjectStates.Clear();
    }

    private void ApplyDamage(float amount)
    {
        if (amount <= 0f)
            return;

        onDamage?.Invoke(amount);

        if (activeDamageReceiver != null && !string.IsNullOrWhiteSpace(damageMethodName))
        {
            activeDamageReceiver.SendMessage(
                damageMethodName,
                amount,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private Transform GetPlayerRootToMove()
    {
        if (playerRootToMoveOverride != null)
            return playerRootToMoveOverride;

        if (activePlayer != null)
            return activePlayer.transform;

        return null;
    }

    private void MovePlayerXZToLastScareCamera()
    {
        Transform playerRoot = GetPlayerRootToMove();

        if (playerRoot == null)
            return;

        Transform finalCameraTransform = GetLastValidScareCameraTransform();

        if (finalCameraTransform == null)
            return;

        Vector3 newPosition = playerRoot.position;

        newPosition.x = finalCameraTransform.position.x;
        newPosition.z = finalCameraTransform.position.z;

        if (!preservePlayerY)
            newPosition.y = finalCameraTransform.position.y;
        else
            newPosition.y = originalPlayerPosition.y;

        newPosition += finalPlayerPositionOffset;

        TeleportPlayerRoot(playerRoot, newPosition);

        if (activePlayerRigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            activePlayerRigidbody.linearVelocity = Vector3.zero;
#else
        activePlayerRigidbody.velocity = Vector3.zero;
#endif
            activePlayerRigidbody.angularVelocity = Vector3.zero;
        }

        Physics.SyncTransforms();
    }

    private void TeleportPlayerRoot(Transform playerRoot, Vector3 newPosition)
    {
        CharacterController[] controllers = playerRoot.GetComponentsInChildren<CharacterController>(true);

        bool[] controllerEnabledStates = new bool[controllers.Length];

        for (int i = 0; i < controllers.Length; i++)
        {
            controllerEnabledStates[i] = controllers[i].enabled;
            controllers[i].enabled = false;
        }

        playerRoot.position = newPosition;

        Physics.SyncTransforms();

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                controllers[i].enabled = controllerEnabledStates[i];
        }
    }

    private Transform GetLastValidScareCameraTransform()
    {
        for (int i = scareBeats.Count - 1; i >= 0; i--)
        {
            if (scareBeats[i] == null)
                continue;

            if (scareBeats[i].cinemachineCameraObject == null)
                continue;

            return scareBeats[i].cinemachineCameraObject.transform;
        }

        return null;
    }
}