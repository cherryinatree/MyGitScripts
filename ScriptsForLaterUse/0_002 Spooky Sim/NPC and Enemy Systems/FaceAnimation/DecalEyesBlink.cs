using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

public class DecalEyesBlink : MonoBehaviour
{
    public DecalProjector leftEye, rightEye;
    public Material eyeTemplate;
    public Texture2D neutral, happy, sad, angry, surprised, thinking, anxious, blink;
    public Vector2 blinkEvery = new Vector2(2.5f, 6f);
    public float blinkDur = 0.08f;

    Material _l, _r; Texture2D _current; float _next; bool _blinking;

    void Awake()
    {
        if (leftEye && eyeTemplate) _l = leftEye.material = new Material(eyeTemplate);
        if (rightEye && eyeTemplate) _r = rightEye.material = new Material(eyeTemplate);
        SetMood("Neutral");
        _next = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
    }

    void Update()
    {
        if (!_blinking && Time.time >= _next) StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        _blinking = true;
        SetEyeTex(blink);
        yield return new WaitForSeconds(blinkDur);
        SetEyeTex(_current);
        _blinking = false;
        _next = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
    }

    void SetEyeTex(Texture2D t) { if (_l) _l.SetTexture("_BaseColorMap", t); if (_r) _r.SetTexture("_BaseColorMap", t); }

    public void SetMood(string mood)
    {
        _current = mood switch
        {
            "Happy" => happy,
            "Sad" => sad,
            "Angry" => angry,
            "Surprised" => surprised,
            "Thinking" => thinking,
            "Anxious" => anxious,
            _ => neutral
        };
        SetEyeTex(_current);
    }
}
