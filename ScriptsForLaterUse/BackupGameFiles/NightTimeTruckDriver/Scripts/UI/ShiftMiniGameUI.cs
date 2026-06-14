using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShiftMiniGameUI : MonoBehaviour
{
    public GameObject miniGamePanel;
    public GameObject miniGameSlider;
    public RectTransform marker;
    public RectTransform sweetSpot;
    public float speed = 300f;
    public AudioSource grindSound;

    private bool goingRight = true;
    private bool isPlaying = false;
    private System.Action<bool> onComplete;

   private bool isSuccess = false;

    public void Start()
    {

        grindSound = GameObject.Find("Truck").GetComponent<AudioSource>();
        miniGamePanel.SetActive(false);
        miniGameSlider.SetActive(false);
    }

    public void StartMiniGame()
    {
        miniGamePanel.SetActive(true);
        miniGameSlider.SetActive(true);
        isPlaying = true;
    }

    public void MoveMarker()
    {

        marker.anchoredPosition += new Vector2((goingRight ? 1 : -1) * speed * Time.deltaTime, 0);

        if (marker.anchoredPosition.x >= 140f) goingRight = false;
        if (marker.anchoredPosition.x <= -140f) goingRight = true;


    }

    public bool ChangeGears()
    {

        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            bool success = IsMarkerInSweetSpot();
            isPlaying = false;

            if (!success && grindSound)
                grindSound.Play();

            miniGameSlider.SetActive(false);
            return success;
        }
        return false;
    }

    bool IsMarkerInSweetSpot()
    {
        float markerLeft = marker.anchoredPosition.x;
        float sweetLeft = sweetSpot.anchoredPosition.x - (sweetSpot.sizeDelta.x / 2f);
        float sweetRight = sweetSpot.anchoredPosition.x + (sweetSpot.sizeDelta.x / 2f);
        return markerLeft >= sweetLeft && markerLeft <= sweetRight;
    }
}
