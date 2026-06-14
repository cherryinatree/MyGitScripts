using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Cherry/Cutscenes/General Cutscene Orchestrator")]
public class GeneralCutsceneOrchestrator : MonoBehaviour
{
    [Serializable]
    public class CutsceneShot
    {
        public string shotName = "Shot";
        [Tooltip("Drag the Cinemachine camera GameObject for this shot here.")]
        public GameObject shotCamera;
        [Min(0.01f)] public float duration = 2f;

        [Header("Events")]
        public UnityEvent onShotStarted;
        public UnityEvent onShotFinished;
    }

    [Header("Cameras")]
    [Tooltip("Your normal gameplay camera to turn back on when the cutscene ends.")]
    [SerializeField] private GameObject gameplayCameraToRestore;

    [Tooltip("The ordered list of cutscene shots.")]
    [SerializeField] private List<CutsceneShot> shots = new();

    [Tooltip("Disable all shot cameras in Awake so gameplay starts on the normal camera.")]
    [SerializeField] private bool disableShotCamerasOnAwake = true;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool playOnlyOnce = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("While Playing")]
    [Tooltip("These behaviours will be disabled during the cutscene, then restored afterward.")]
    [SerializeField] private Behaviour[] disableWhilePlaying;

    [Tooltip("These GameObjects will be hidden during the cutscene, then restored afterward.")]
    [SerializeField] private GameObject[] hideWhilePlaying;

    [Tooltip("These GameObjects will be shown during the cutscene, then restored afterward.")]
    [SerializeField] private GameObject[] showWhilePlaying;

    [Header("Events")]
    public UnityEvent onCutsceneStarted;
    public UnityEvent onCutsceneFinished;

    public bool IsPlaying { get; private set; }
    public bool HasPlayed { get; private set; }

    public event Action Finished;

    private Coroutine _playRoutine;
    private bool[] _behaviourStates;
    private bool[] _hideStates;
    private bool[] _showStates;

    private void Awake()
    {
        if (disableShotCamerasOnAwake)
        {
            DeactivateAllShotCameras();
        }

        if (gameplayCameraToRestore != null && !IsPlaying)
        {
            gameplayCameraToRestore.SetActive(true);
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (IsPlaying) return;
        if (playOnlyOnce && HasPlayed) return;

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void PlayFromBeginning()
    {
        if (IsPlaying) return;
        HasPlayed = false;
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void StopAndRestore()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        FinishCutscene();
    }

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;
        HasPlayed = true;

        CacheAndApplyTemporaryState();

        if (gameplayCameraToRestore != null)
        {
            gameplayCameraToRestore.SetActive(false);
        }

        DeactivateAllShotCameras();
        onCutsceneStarted?.Invoke();

        for (int i = 0; i < shots.Count; i++)
        {
            CutsceneShot shot = shots[i];
            if (shot == null) continue;

            ActivateOnlyShotCamera(shot.shotCamera);

            shot.onShotStarted?.Invoke();

            yield return WaitForSecondsCutscene(shot.duration);

            shot.onShotFinished?.Invoke();
        }

        _playRoutine = null;
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        DeactivateAllShotCameras();

        if (gameplayCameraToRestore != null)
        {
            gameplayCameraToRestore.SetActive(true);
        }

        RestoreTemporaryState();

        IsPlaying = false;

        onCutsceneFinished?.Invoke();
        Finished?.Invoke();
    }

    private IEnumerator WaitForSecondsCutscene(float seconds)
    {
        if (seconds <= 0f)
            yield break;

        float endTime = (useUnscaledTime ? Time.unscaledTime : Time.time) + seconds;

        while ((useUnscaledTime ? Time.unscaledTime : Time.time) < endTime)
        {
            yield return null;
        }
    }

    private void ActivateOnlyShotCamera(GameObject activeCamera)
    {
        for (int i = 0; i < shots.Count; i++)
        {
            if (shots[i] == null || shots[i].shotCamera == null) continue;
            shots[i].shotCamera.SetActive(shots[i].shotCamera == activeCamera);
        }
    }

    private void DeactivateAllShotCameras()
    {
        for (int i = 0; i < shots.Count; i++)
        {
            if (shots[i] == null || shots[i].shotCamera == null) continue;
            shots[i].shotCamera.SetActive(false);
        }
    }

    private void CacheAndApplyTemporaryState()
    {
        _behaviourStates = new bool[disableWhilePlaying != null ? disableWhilePlaying.Length : 0];
        for (int i = 0; i < _behaviourStates.Length; i++)
        {
            if (disableWhilePlaying[i] == null) continue;
            _behaviourStates[i] = disableWhilePlaying[i].enabled;
            disableWhilePlaying[i].enabled = false;
        }

        _hideStates = new bool[hideWhilePlaying != null ? hideWhilePlaying.Length : 0];
        for (int i = 0; i < _hideStates.Length; i++)
        {
            if (hideWhilePlaying[i] == null) continue;
            _hideStates[i] = hideWhilePlaying[i].activeSelf;
            hideWhilePlaying[i].SetActive(false);
        }

        _showStates = new bool[showWhilePlaying != null ? showWhilePlaying.Length : 0];
        for (int i = 0; i < _showStates.Length; i++)
        {
            if (showWhilePlaying[i] == null) continue;
            _showStates[i] = showWhilePlaying[i].activeSelf;
            showWhilePlaying[i].SetActive(true);
        }
    }

    private void RestoreTemporaryState()
    {
        for (int i = 0; i < (_behaviourStates?.Length ?? 0); i++)
        {
            if (disableWhilePlaying[i] == null) continue;
            disableWhilePlaying[i].enabled = _behaviourStates[i];
        }

        for (int i = 0; i < (_hideStates?.Length ?? 0); i++)
        {
            if (hideWhilePlaying[i] == null) continue;
            hideWhilePlaying[i].SetActive(_hideStates[i]);
        }

        for (int i = 0; i < (_showStates?.Length ?? 0); i++)
        {
            if (showWhilePlaying[i] == null) continue;
            showWhilePlaying[i].SetActive(_showStates[i]);
        }
    }
}