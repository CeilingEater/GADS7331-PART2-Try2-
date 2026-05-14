using UnityEngine;
using TMPro; // Needed for TextMeshPro
using UnityEngine.UI;
using System.Collections.Generic;

public class StoryManager : MonoBehaviour {
    [Header("UI References")]
    public TMP_Text storyDisplayText;
    public TMP_InputField playerInputField;
    public Button sendButton;

    [Header("Settings")]
    public string modelName = "gemma3:1b";
    
    private OllamaClient _client = new OllamaClient();
    private List<ChatMessage> _chatHistory = new List<ChatMessage>();

    void Start() {
        // 1. Setup Initial Rules
        _chatHistory.Add(new ChatMessage { 
            role = "system", 
            content = "You are a DnD DM. Keep responses brief. Output ONLY JSON: {\"narrative\": \"text\"}" 
        });

        // 2. Link the Button
        sendButton.onClick.AddListener(OnSendClicked);
        
        storyDisplayText.text = "Welcome, adventurer. What do you do?";
    }

    async void OnSendClicked() {
        string userInput = playerInputField.text;
        if (string.IsNullOrWhiteSpace(userInput)) return;

        // Disable UI while AI is thinking
        sendButton.interactable = false;
        storyDisplayText.text += $"\n\n<b>You:</b> {userInput}";
        playerInputField.text = "";

        // Add user message to history
        _chatHistory.Add(new ChatMessage { role = "user", content = userInput });

        var request = new ChatRequest {
            model = modelName,
            messages = _chatHistory.ToArray()
        };

        string aiRawJson = await _client.GetAiReply(request);
        
        if (!string.IsNullOrEmpty(aiRawJson)) {
            string cleanedJson = CleanJson(aiRawJson);
            try {
                StoryNode node = JsonUtility.FromJson<StoryNode>(cleanedJson);
                
                // Add AI response to UI and history
                storyDisplayText.text += $"\n\n<b>DM:</b> {node.narrative}";
                _chatHistory.Add(new ChatMessage { role = "assistant", content = aiRawJson });
            } catch {
                Debug.LogError("Failed to parse AI response: " + aiRawJson);
            }
        }

        sendButton.interactable = true;
    }

    private string CleanJson(string input) {
        int start = input.IndexOf('{');
        int end = input.LastIndexOf('}');
        return (start != -1 && end != -1) ? input.Substring(start, (end - start) + 1) : input;
    }
}