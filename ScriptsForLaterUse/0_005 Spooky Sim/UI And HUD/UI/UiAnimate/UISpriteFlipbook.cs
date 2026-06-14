using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UISpriteFlipbook : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float fps = 12f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool randomStartFrame = true;

    private Image img;
    private float t0;

    private void Awake()
    {

        img = GetComponent<Image>();
        t0 = (useUnscaledTime ? Time.unscaledTime : Time.time);

        if (randomStartFrame && frames != null && frames.Length > 0)
            img.sprite = frames[Random.Range(0, frames.Length)];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        float t = (useUnscaledTime ? Time.unscaledTime : Time.time) - t0;
        int index = (int)(t * fps) % frames.Length;
        img.sprite = frames[index];
    }
}
