using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO; // Needed for Saving to Files
using System;

[Serializable]
public class SaveData {
    public List<ChatMessage> history;
}

public class StoryManager : MonoBehaviour {
    [Header("UI References")]
    public TMP_Text storyDisplayText;
    public TMP_InputField playerInputField;
    public Button sendButton;
    public Button rollButton; // Add a button for the d20 roll
    public ScrollRect scrollRect;

    [Header("Settings")]
    public string modelName = "gemma3:1b";
    
    private OllamaClient _client = new OllamaClient();
    private List<ChatMessage> _chatHistory = new List<ChatMessage>();
    private string _savePath;

    void Start() {
        _savePath = Path.Combine(Application.persistentDataPath, "adventure_save.json");
        
        // Setup Buttons
        sendButton.onClick.AddListener(() => OnSendClicked());
        rollButton.onClick.AddListener(OnRollClicked);

        // Try to load previous game, otherwise start fresh
        if (File.Exists(_savePath)) {
            LoadGame();
        } else {
            StartNewGame();
        }
    }

    async void StartNewGame() {
        _chatHistory.Clear();
        _chatHistory.Add(new ChatMessage { 
            role = "system", 
            content = "You are a DnD DM. Start with 3 short scenarios. " +
                      "When the player rolls a d20, narrate success if high (20 is crit), failure if low (1 is crit fail). " +
                      "Output ONLY JSON: {\"narrative\": \"text\"}" 
        });

        storyDisplayText.text = "Beginning a new legend...";
        await GetAiResponse("Give me my 3 starting scenarios.");
    }

    async void OnSendClicked() {
        string input = playerInputField.text;
        if (string.IsNullOrWhiteSpace(input)) return;

        AddTextToDisplay($"<b>You:</b> {input}", "#5fbff9");
        playerInputField.text = "";
        await GetAiResponse(input);
        SaveGame();
    }

    async void OnRollClicked() {
        int roll = UnityEngine.Random.Range(1, 21); // 1 to 20
        string action = string.IsNullOrEmpty(playerInputField.text) ? "my current action" : playerInputField.text;
        
        string rollMessage = $"I roll a d20 for '{action}' and I get a {roll}!";
        AddTextToDisplay($"🎲 <b>Roll:</b> {roll}", "#ffcf33");
        
        playerInputField.text = ""; // Clear input after rolling
        await GetAiResponse(rollMessage);
        SaveGame();
    }

    async System.Threading.Tasks.Task GetAiResponse(string prompt) {
        sendButton.interactable = false;
        rollButton.interactable = false;

        _chatHistory.Add(new ChatMessage { role = "user", content = prompt });
        var request = new ChatRequest { model = modelName, messages = _chatHistory.ToArray() };

        string aiRawJson = await _client.GetAiReply(request);
        if (!string.IsNullOrEmpty(aiRawJson)) {
            StoryNode node = JsonUtility.FromJson<StoryNode>(CleanJson(aiRawJson));
            AddTextToDisplay($"<b>DM:</b> {node.narrative}", "#ffffff");
            _chatHistory.Add(new ChatMessage { role = "assistant", content = aiRawJson });
        }

        sendButton.interactable = true;
        rollButton.interactable = true;
    }

    void AddTextToDisplay(string text, string hexColor) {
        storyDisplayText.text += $"\n\n<color={hexColor}>{text}</color>";
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // --- SAVE & LOAD LOGIC ---
    public void SaveGame() {
        SaveData data = new SaveData { history = _chatHistory };
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(_savePath, json);
        Debug.Log("Game Saved to: " + _savePath);
    }

    public void LoadGame() {
        string json = File.ReadAllText(_savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        _chatHistory = data.history;

        storyDisplayText.text = "--- Adventure Resumed ---";
        foreach (var msg in _chatHistory) {
            if (msg.role == "user") AddTextToDisplay("<b>You:</b> " + msg.content, "#5fbff9");
            if (msg.role == "assistant") {
                var node = JsonUtility.FromJson<StoryNode>(CleanJson(msg.content));
                AddTextToDisplay("<b>DM:</b> " + node.narrative, "#ffffff");
            }
        }
    }

    private string CleanJson(string input) {
        int start = input.IndexOf('{');
        int end = input.LastIndexOf('}');
        return (start != -1 && end != -1) ? input.Substring(start, (end - start) + 1) : input;
    }
}