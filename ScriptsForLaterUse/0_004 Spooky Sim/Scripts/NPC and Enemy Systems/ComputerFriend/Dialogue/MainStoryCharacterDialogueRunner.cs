using UnityEngine;
using DialogueEditor;
using NUnit.Framework;
using System.Collections.Generic;

public class MainStoryCharacterDialogueRunner : MonoBehaviour
{

    public static MainStoryCharacterDialogueRunner Instance;

    public NPCConversation firstLine;
    public List<NPCConversation> conversations;
    private DecalMouthAnimator decalMouthAnimator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
        if(SaveData.Current.mainData.progressionData.computerFriendDialogueTracker == null)
        {
            SaveData.Current.mainData.progressionData.computerFriendDialogueTracker = new AiDialogueTracker();
            SaveData.Current.mainData.progressionData.computerFriendDialogueTracker.firstLineSpoken = false;
            SaveData.Current.mainData.progressionData.computerFriendDialogueTracker.dialogueIDsSpoken = new System.Collections.Generic.List<int>();
        }
        decalMouthAnimator = GetComponent<DecalMouthAnimator>();
        //ConversationManager.Instance.StartConversation(firstLine);
    }

    public void PlayOpeningLine()
    {

       if (!SaveData.Current.mainData.progressionData.computerFriendDialogueTracker.firstLineSpoken)
        {
            
            Debug.Log("Starting First Line Conversation: " + ConversationManager.Instance);

            ConversationManager.Instance.StartConversation(firstLine);
            decalMouthAnimator.PlayById("0", null);
            SaveData.Current.mainData.progressionData.computerFriendDialogueTracker.firstLineSpoken = true;
            FindFirstObjectByType<WorldHintService>().ShowInFront(Camera.main, "Press 'WASD' to Move.", 0, 5);
        }
    }

    public void ContinueConversation()
    {
       ConversationManager.Instance.SelectNextOption();
        ConversationManager.Instance.PressSelectedOption();
    }

    public void StartConversationByID(int id)
    {
        foreach (int spokenID in SaveData.Current.mainData.progressionData.computerFriendDialogueTracker.dialogueIDsSpoken)
        {
            if (spokenID == id)
            {
                return;
            }
        }
        
        ConversationManager.Instance.StartConversation(conversations[id]);
    }
}
