using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // TextMeshPro is recommended
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public Transform optionsParent;
    public Button optionButtonPrefab;
    public AudioSource audioSource;

    private DialogueData currentDialogue;
    private CoreNPC currentNPC;
    private int currentPageIndex;
    private bool pageDisplaying;
    public GameObject player;

    private void Awake() { Instance = this;


    }

    private void Start()
    {

      //  Cursor.lockState = CursorLockMode.Locked;
      //  Cursor.visible = false;
        player = GameObject.FindWithTag("Player");
    }

    public void StartDialogue(CoreNPC npc, DialogueData dialogue)
    {
        currentNPC = npc;
        currentDialogue = dialogue;
        currentPageIndex = 0;
        dialogueBox.SetActive(true);
        DisplayPage(currentDialogue.pages[currentPageIndex]);

        //Cursor.lockState = CursorLockMode.None;
       // Cursor.visible = true;
        player.transform.LookAt(npc.transform);
        player.GetComponent<LookAction>().LockCamera(true);
        
    }

    void DisplayPage(DialoguePage page)
    {
        StopAllCoroutines();
        StartCoroutine(TypeSentence(page));
    }

    IEnumerator TypeSentence(DialoguePage page)
    {
        pageDisplaying = true;
        dialogueText.text = "";
        foreach (string sentence in page.sentences)
        {
            foreach (char c in sentence.ToCharArray())
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(0.01f); // typing speed
            }
            dialogueText.text += "\n";
        }

        // Play voice clip
        if (page.voiceClip != null && audioSource != null)
        {
            audioSource.clip = page.voiceClip;
            audioSource.Play();
        }

        pageDisplaying = false;
        Debug.Log("Finished displaying page.");
        // Show options only on last page
        if (page.options != null && page.options.Length > 0)
        {
            Debug.Log("Show Options.");
            ShowOptions(page.options);
        }
    }

    void ShowOptions(DialogueOption[] options)
    {
        foreach (Transform child in optionsParent)
            Destroy(child.gameObject);

        foreach (var option in options)
        {
            Button btn = Instantiate(optionButtonPrefab, optionsParent);
            btn.gameObject.SetActive(true);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = option.optionText;
            btn.onClick.AddListener(() =>
            {
                option.ExecuteOption(currentNPC); 
                EndDialogue();
            });
        }
    }

    public void NextPage()
    {
        if (pageDisplaying) return; // cannot skip while typing if desired

        if (currentPageIndex + 1 < currentDialogue.pages.Length)
        {
            currentPageIndex++;
            DisplayPage(currentDialogue.pages[currentPageIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialogueBox.SetActive(false);
        audioSource.Stop();
       // Cursor.lockState = CursorLockMode.Locked;
       // Cursor.visible = false;
        player.GetComponent<LookAction>().LockCamera(false);
    }

    // Optional: connect NextPage to input system
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && dialogueBox.activeSelf && currentPageIndex + 1 < currentDialogue.pages.Length)
        {
            NextPage();
        }
    }
}
