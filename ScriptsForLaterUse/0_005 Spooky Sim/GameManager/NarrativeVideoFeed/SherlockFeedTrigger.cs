using UnityEngine;

public class SherlockFeedTrigger : MonoBehaviour
{
    [SerializeField] private SherlockFeedDefinition feed;
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool onceOnly = true;
    [SerializeField] private string playerTag = "Player";

    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;
        if (_triggered && onceOnly) return;
        if (!other.CompareTag(playerTag)) return;

        TriggerFeed();
    }

    public void TriggerFeed()
    {
        if (_triggered && onceOnly) return;
        if (SherlockFeedManager.Instance == null || feed == null) return;

        bool played = SherlockFeedManager.Instance.TryPlay(feed);
        if (played)
            _triggered = true;
    }
}