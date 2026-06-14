using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.InputSystem;

public class SherlockFeedManager : MonoBehaviour
{
    public static SherlockFeedManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Optional. If assigned, the VideoPlayer will render here and the RawImage will use it.")]
    [SerializeField] private RenderTexture targetTexture;
    [Tooltip("Optional. If assigned, video audio will be routed through this AudioSource.")]
    [SerializeField] private AudioSource videoAudioSource;
    [SerializeField] private bool clearTextureWhenClosed = false;

    [Header("Input System")]
    [Tooltip("Bind this to your Skip / Submit / Advance Dialogue action.")]
    [SerializeField] private InputActionReference skipAction;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Optional Player Lock")]
    [Tooltip("Optional reference to something that can disable player input while Sherlock speaks.")]
    [SerializeField] private MonoBehaviour playerInputBlocker;

    private readonly Queue<SherlockFeedDefinition> _queue = new Queue<SherlockFeedDefinition>();
    private readonly HashSet<string> _playedFeeds = new HashSet<string>();
    private readonly HashSet<string> _progressFlags = new HashSet<string>();

    private SherlockFeedDefinition _currentFeed;
    private SherlockFeedDefinition _priorityPendingFeed;
    private Coroutine _playRoutine;
    private Coroutine _stopRoutine;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        ConfigureVideoPlayer();
    }

    private void OnEnable()
    {
        if (skipAction != null && skipAction.action != null)
            skipAction.action.Enable();
    }

    private void OnDisable()
    {
        if (skipAction != null && skipAction.action != null)
            skipAction.action.Disable();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Update()
    {
        if (!_isPlaying || _currentFeed == null || !_currentFeed.skippable)
            return;

        if (skipAction != null && skipAction.action != null && skipAction.action.WasPressedThisFrame())
        {
            StopCurrentFeed(markComplete: true);
        }
    }

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.source = VideoSource.VideoClip;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        if (targetTexture != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = targetTexture;

            if (videoImage != null)
                videoImage.texture = targetTexture;
        }
        else
        {
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
        }

        if (videoAudioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }
    }

    public bool HasFlag(string flag) => !string.IsNullOrWhiteSpace(flag) && _progressFlags.Contains(flag);

    public void SetFlag(string flag)
    {
        if (!string.IsNullOrWhiteSpace(flag))
            _progressFlags.Add(flag);
    }

    public bool HasPlayed(string feedId) => !string.IsNullOrWhiteSpace(feedId) && _playedFeeds.Contains(feedId);

    public bool TryPlay(SherlockFeedDefinition feed)
    {
        if (feed == null) return false;
        if (!CanPlay(feed)) return false;

        if (_isPlaying)
        {
            if (_currentFeed != null && feed.priority > _currentFeed.priority)
            {
                _priorityPendingFeed = feed;
                StopCurrentFeed(markComplete: false);
                return true;
            }

            _queue.Enqueue(feed);
            return true;
        }

        StartFeed(feed);
        return true;
    }

    public bool CanPlay(SherlockFeedDefinition feed)
    {
        if (feed == null) return false;

        if (feed.playOnce && !string.IsNullOrWhiteSpace(feed.feedId) && _playedFeeds.Contains(feed.feedId))
            return false;

        if (feed.requiredFlags != null)
        {
            for (int i = 0; i < feed.requiredFlags.Count; i++)
            {
                string flag = feed.requiredFlags[i];
                if (!string.IsNullOrWhiteSpace(flag) && !_progressFlags.Contains(flag))
                    return false;
            }
        }

        return true;
    }

    private void StartFeed(SherlockFeedDefinition feed)
    {
        _currentFeed = feed;

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(PlayRoutine(feed));
    }

    private IEnumerator PlayRoutine(SherlockFeedDefinition feed)
    {
        _isPlaying = true;

        if (feed.setFlagsOnStart != null)
        {
            foreach (var flag in feed.setFlagsOnStart)
                SetFlag(flag);
        }

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(feed.displayTitle) ? "Sherlock" : feed.displayTitle;

        if (subtitleText != null)
            subtitleText.text = feed.subtitleText;

        if (feed.blockPlayerInput)
            SetPlayerBlocked(true);

        yield return FadeCanvas(1f);

        bool waitingForVideo = false;

        if (videoPlayer != null && feed.videoClip != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = feed.videoClip;
            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared)
                yield return null;

            if (targetTexture == null && videoImage != null)
                videoImage.texture = videoPlayer.texture;

            videoPlayer.Play();
            waitingForVideo = true;
        }
        else
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, feed.fallbackDuration));
        }

        if (waitingForVideo)
        {
            while (_isPlaying && videoPlayer != null && videoPlayer.isPlaying)
                yield return null;
        }

        StopCurrentFeed(markComplete: true);
    }

    private void StopCurrentFeed(bool markComplete)
    {
        if (!_isPlaying) return;

        SherlockFeedDefinition finishedFeed = _currentFeed;

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (_stopRoutine != null)
            StopCoroutine(_stopRoutine);

        _stopRoutine = StartCoroutine(StopRoutine(finishedFeed, markComplete));
    }

    private IEnumerator StopRoutine(SherlockFeedDefinition finishedFeed, bool markComplete)
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        yield return FadeCanvas(0f);

        if (clearTextureWhenClosed && videoImage != null)
            videoImage.texture = null;

        if (finishedFeed != null && markComplete)
        {
            if (finishedFeed.playOnce && !string.IsNullOrWhiteSpace(finishedFeed.feedId))
                _playedFeeds.Add(finishedFeed.feedId);

            if (finishedFeed.setFlagsOnComplete != null)
            {
                foreach (var flag in finishedFeed.setFlagsOnComplete)
                    SetFlag(flag);
            }
        }

        if (finishedFeed != null && finishedFeed.blockPlayerInput)
            SetPlayerBlocked(false);

        _currentFeed = null;
        _isPlaying = false;
        _stopRoutine = null;

        if (_priorityPendingFeed != null)
        {
            SherlockFeedDefinition nextPriority = _priorityPendingFeed;
            _priorityPendingFeed = null;

            if (CanPlay(nextPriority))
            {
                StartFeed(nextPriority);
                yield break;
            }
        }

        while (_queue.Count > 0)
        {
            SherlockFeedDefinition next = _queue.Dequeue();
            if (CanPlay(next))
            {
                StartFeed(next);
                yield break;
            }
        }
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (_isPlaying)
            StopCurrentFeed(markComplete: true);
    }

    private IEnumerator FadeCanvas(float target)
    {
        if (rootGroup == null)
            yield break;

        rootGroup.blocksRaycasts = target > 0.01f;
        rootGroup.interactable = target > 0.01f;

        float start = rootGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Lerp(start, target, time / Mathf.Max(0.0001f, fadeDuration));
            yield return null;
        }

        rootGroup.alpha = target;
    }

    private void SetPlayerBlocked(bool blocked)
    {
        if (playerInputBlocker == null) return;
        playerInputBlocker.enabled = !blocked;
    }

    public void SaveFlagsAndPlayed(out List<string> flags, out List<string> playedFeeds)
    {
        flags = new List<string>(_progressFlags);
        playedFeeds = new List<string>(_playedFeeds);
    }

    public void LoadFlagsAndPlayed(List<string> flags, List<string> playedFeeds)
    {
        _progressFlags.Clear();
        _playedFeeds.Clear();

        if (flags != null)
        {
            foreach (var flag in flags)
                SetFlag(flag);
        }

        if (playedFeeds != null)
        {
            foreach (var id in playedFeeds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _playedFeeds.Add(id);
            }
        }
    }
}