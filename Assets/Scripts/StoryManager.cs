using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement; // Needed to go back to menu

 public class SaveData {
    public List<ChatMessage> history;
}

public class StoryManager : MonoBehaviour {
    [Header("UI References")]
    public TMP_Text storyDisplayText;
    public TMP_InputField playerInputField;
    public Button rollButton; 
    public ScrollRect scrollRect;

    [Header("Settings")]
    public string modelName = "gemma3:1b";
    
    private OllamaClient _client = new OllamaClient();
    private List<ChatMessage> _chatHistory = new List<ChatMessage>();
    private string _savePath;

    void Start() {
        // Get the filename set by the Menu Scene
        string fileName = GameDataHandler.CurrentSaveFile;
        
        // If we opened this scene directly without the menu, give it a default name
        if (string.IsNullOrEmpty(fileName)) fileName = "QuickSave.json";
        
        _savePath = Path.Combine(Application.persistentDataPath, fileName);

        // Listen for the 'Enter' key on the input field
        playerInputField.gameObject.SetActive(true); 
        if (rollButton != null) rollButton.gameObject.SetActive(false);

        playerInputField.onSubmit.AddListener((value) => OnInputSubmitted(value));
        if (rollButton != null) rollButton.onClick.AddListener(OnRollClicked);
            

        if (File.Exists(_savePath)) {
            LoadGame();
        } else {
            StartNewGame();
        }
    }

    private void OnInputSubmitted(string input) {
        if (string.IsNullOrWhiteSpace(input)) return;
        
        // Clear input and keep it focused so the player can keep typing
        playerInputField.text = "";
        playerInputField.ActivateInputField(); 
        
        ExecuteStep(input);
    }

    async void ExecuteStep(string userInput) {
        AddTextToDisplay($"<b>You:</b> {userInput}", "#5fbff9");
        await GetAiResponse(userInput);
        SaveGame();
    }

    public async void OnRollClicked() {
        int roll = UnityEngine.Random.Range(1, 21);
        string rollMessage = $"I roll a d20 and get a {roll}";
        
        AddTextToDisplay($"ROLL: {roll}", "#ffcf33");
        
        rollButton.gameObject.SetActive(false);
        playerInputField.gameObject.SetActive(true);
        
        await GetAiResponse(rollMessage);
        SaveGame();
    }

    async System.Threading.Tasks.Task GetAiResponse(string prompt) {
        // 1. Show the user that the AI is thinking (optional UI feedback)
        if (rollButton != null) rollButton.interactable = false;

        _chatHistory.Add(new ChatMessage { role = "user", content = prompt });
        var request = new ChatRequest { model = modelName, messages = _chatHistory.ToArray() };

        string aiRawJson = await _client.GetAiReply(request);
    
        if (!string.IsNullOrEmpty(aiRawJson)) {
            string cleaned = CleanJson(aiRawJson);
            try {
                StoryNode node = JsonUtility.FromJson<StoryNode>(cleaned);
                AddTextToDisplay($"<b>DM:</b> {node.narrative}", "#ffffff");

                // --- FIX: Explicitly toggle both UI elements ---
                // If a roll is required, show the button and hide the input.
                // If no roll is required, show the input and hide the button.
                bool needsRoll = node.requiresRoll;
            
                playerInputField.gameObject.SetActive(!needsRoll);
                rollButton.gameObject.SetActive(needsRoll);

                // Re-focus the input field if it's the one active
                if (!needsRoll) playerInputField.ActivateInputField();

                _chatHistory.Add(new ChatMessage { role = "assistant", content = aiRawJson });
            } catch {
                Debug.LogError("AI sent bad JSON: " + aiRawJson);
                // Default back to input if the AI breaks
                playerInputField.gameObject.SetActive(true);
            }
        }

        if (rollButton != null) rollButton.interactable = true;
    }

    void AddTextToDisplay(string text, string hexColor) {
        storyDisplayText.text += $"\n\n<color={hexColor}>{text}</color>";
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    // Call this from a UI Button to go back home
    public void GoToMenu() {
        SaveGame();
        SceneManager.LoadScene("Saves"); // Change this to your exact Menu scene name
    }

    // --- SAVE/LOAD LOGIC ---
    public void SaveGame() {
        string json = JsonUtility.ToJson(new SaveData { history = _chatHistory });
        File.WriteAllText(_savePath, json);
    }

    public void LoadGame() {
        if (!File.Exists(_savePath)) return;
        string json = File.ReadAllText(_savePath);
        _chatHistory = JsonUtility.FromJson<SaveData>(json).history;

        storyDisplayText.text = "--- Adventure Resumed ---";
        foreach (var msg in _chatHistory) {
            if (msg.role == "user") AddTextToDisplay("<b>You:</b> " + msg.content, "#5fbff9");
            if (msg.role == "assistant") {
                StoryNode node = JsonUtility.FromJson<StoryNode>(CleanJson(msg.content));
                AddTextToDisplay("<b>DM:</b> " + node.narrative, "#ffffff");
            }
        }
    }

    private async void StartNewGame() {
        // We add a tiny bit more detail to the system prompt to ensure the first JSON 
        // has requiresRoll set to false so the player can type their choice.
        string systemInstructions = "You are a creative DM. Rules:\n" +
                                    "1. Output ONLY JSON: {\"narrative\": \"...\", \"requiresRoll\": false}\n" +
                                    "2. Present 3 HIGHLY DISTINCT scenarios for the player to choose from (e.g. Cyberpunk, Underwater, Sky Temple).\n" +
                                    "3. Set 'requiresRoll' to true ONLY for skill-based actions, meaning things that are hard to do. Opt for making the player roll as much as possible for each response unless the player does something non-skilled base such as talking to someone. The intro should be 'requiresRoll': false." +
                                    "4. After narration of the story, give the player only TWO options to choose from.";

        _chatHistory.Add(new ChatMessage { role = "system", content = systemInstructions });
        await GetAiResponse("Start the game.");
    }

    private string CleanJson(string input) {
        int start = input.IndexOf('{');
        int end = input.LastIndexOf('}');
        return (start != -1 && end != -1) ? input.Substring(start, (end - start) + 1) : input;
    }
}