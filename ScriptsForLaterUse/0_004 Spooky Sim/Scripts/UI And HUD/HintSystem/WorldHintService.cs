using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WorldHintService : MonoBehaviour
{
    public static WorldHintService I { get; private set; }
    public WorldHintPanel panelPrefab;

    public Sprite[] controlImages;

    WorldHintPanel _active;   // simple single-panel approach; expand to a pool if needed

    void Start()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        // DontDestroyOnLoad(this); // enable if you want it global
        _active = FindFirstObjectByType<WorldHintPanel>();
        //if (_active != null) _active.gameObject.SetActive(false);
        if (_active != null) Hide();

    }

    WorldHintPanel Ensure()
    {
        if(_active != null)
        {
            if (!_active.gameObject.activeSelf) _active.gameObject.SetActive(true);
        }
        if (_active && _active.gameObject) return _active;
        _active = Instantiate(panelPrefab);
        return _active;
    }

    public void ShowInFront(Camera cam, string text, int imageNumber, float distance = 3f, float autoHideSeconds = -1f)
    {
        _active.Show();
        var p = Ensure();
        p.ConfigureInFront(cam, distance, text);
        if(imageNumber >= 0 && imageNumber < controlImages.Length)
        {
            p.icon.gameObject.SetActive(true);
            p.icon.sprite = controlImages[imageNumber];
        }
        else
        {

           p.icon.sprite = null;
           p.icon.gameObject.SetActive(false);
        }
        if (autoHideSeconds > 0f) p.HideAfter(autoHideSeconds);
    }
    public void ShowInFront(Camera cam, string text, int imageNumber, float autoHideSeconds = -1f)
    {
        _active.Show();

        float distance = 3f;
        var p = Ensure();
        p.ConfigureInFront(cam, distance, text);
        if (imageNumber >= 0 && imageNumber < controlImages.Length)
        {
            p.icon.gameObject.SetActive(true);
            p.icon.sprite = controlImages[imageNumber];
        }
        else
        {

            p.icon.sprite = null;
            p.icon.gameObject.SetActive(false);
        }
        if (autoHideSeconds > 0f) p.HideAfter(autoHideSeconds);
    }


    public void ShowNextTo(Camera cam, Transform target, string text, Vector3 localOffset, int imageNumber, float autoHideSeconds = -1f)
    {
        _active.Show();
        var p = Ensure();
        p.ConfigureNextTo(cam, target, localOffset, text);
        if (imageNumber >= 0 && imageNumber < controlImages.Length)
        {
            p.icon.sprite = controlImages[imageNumber];
        }
        else
        {
            p.icon.sprite = null;
            p.icon.gameObject.SetActive(false);
        }
        if (autoHideSeconds > 0f) p.HideAfter(autoHideSeconds);
    }

    public void ShowActionPromptNextTo(Camera cam, Transform target, string prefix, InputActionReference action, Vector3 localOffset, float autoHideSeconds = -1f)
    {
        _active.Show();
        var p = Ensure();
        p.ConfigureNextTo(cam, target, localOffset, "");
        p.SetActionPrompt(prefix, action);
        if (autoHideSeconds > 0f) p.HideAfter(autoHideSeconds);
    }

    public void Hide() { if (_active) _active.Hide(); }
}
