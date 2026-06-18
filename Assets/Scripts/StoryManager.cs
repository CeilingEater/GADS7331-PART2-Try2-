using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

public class SaveData {
    public List<ChatMessage> history;
}

public class StoryManager : MonoBehaviour {
    [Header("UI References")]
    public TMP_Text storyDisplayText;
    public TMP_InputField playerInputField;
    public ScrollRect scrollRect;

    [Header("Dynamic Option UI")]
    public GameObject optionButtonPrefab; 
    public Transform optionButtonContainer; 

    [Header("Settings")]
    public string modelName = "gemma3:1b";
    
    private OllamaClient _client = new OllamaClient();
    private List<ChatMessage> _chatHistory = new List<ChatMessage>();
    private string _savePath;
    private List<GameObject> _activeButtons = new List<GameObject>();

    [System.Serializable]
    public class GameOption {
        public string text;
        public bool requiresRoll;
    }

    [System.Serializable]
    public class StoryNode {
        public string narrative;
        public GameOption[] options; 
    }

    void Start() {
        string fileName = GameDataHandler.CurrentSaveFile;
        if (string.IsNullOrEmpty(fileName)) fileName = "QuickSave.json";
        
        _savePath = Path.Combine(Application.persistentDataPath, fileName);

        playerInputField.gameObject.SetActive(true); 
        playerInputField.onSubmit.AddListener((value) => OnInputSubmitted(value));

        if (File.Exists(_savePath)) {
            LoadGame();
        } else {
            StartNewGame();
        }
    }

    private void OnInputSubmitted(string input) {
        if (string.IsNullOrWhiteSpace(input)) return;
        playerInputField.text = "";
        playerInputField.ActivateInputField(); 
        ExecuteStep(input);
    }

    async void ExecuteStep(string userInput) {
        AddTextToDisplay($"<b>You:</b> {userInput}", "#5fbff9");
        await GetAiResponse(userInput);
        SaveGame();
    }

    async System.Threading.Tasks.Task GetAiResponse(string prompt) {
        _chatHistory.Add(new ChatMessage { role = "user", content = prompt });
        var request = new ChatRequest { model = modelName, messages = _chatHistory.ToArray() };

        string aiRawJson = await _client.GetAiReply(request);
        ClearOptionButtons();

        if (!string.IsNullOrEmpty(aiRawJson)) {
            string cleaned = CleanJson(aiRawJson);
            try {
                StoryNode node = JsonUtility.FromJson<StoryNode>(cleaned);
                AddTextToDisplay($"<b>DM:</b> {node.narrative}", "#ffffff");

                // Always keep input field open for custom typed user entries
                playerInputField.gameObject.SetActive(true);
                playerInputField.ActivateInputField();

                if (node.options != null && node.options.Length > 0) {
                    foreach (GameOption option in node.options) {
                        CreateOptionButton(option);
                    }
                }

                _chatHistory.Add(new ChatMessage { role = "assistant", content = aiRawJson });
            } catch {
                Debug.LogError("AI sent bad JSON: " + aiRawJson);
                AddTextToDisplay("<b>DM:</b> [The narrative fractured... try entering a custom action below.]", "#ff4444");
            }
        }
    }

    void CreateOptionButton(GameOption option) {
        if (optionButtonPrefab == null || optionButtonContainer == null) return;

        GameObject btnObj = Instantiate(optionButtonPrefab, optionButtonContainer);
        _activeButtons.Add(btnObj);

        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        Button btn = btnObj.GetComponent<Button>();

        // Style and configure button behavior depending on risk profile
        if (option.requiresRoll) {
            if (btnText != null) btnText.text = $"{option.text} 🎲";
            
            // Visual Indicator: Turn the button background a soft crimson/amber risk tint
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.85f, 0.4f, 0.4f, 1f); // Soft red alert
            colors.selectedColor = new Color(0.95f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            // When clicked, handle roll first, then transmit to DM
            btn.onClick.AddListener(() => {
                int roll = UnityEngine.Random.Range(1, 21);
                AddTextToDisplay($"ROLL: {roll}", "#ffcf33");
                ExecuteStep($"{option.text} (I roll a d20 and get a {roll})");
            });
        } else {
            if (btnText != null) btnText.text = option.text;
            btn.onClick.AddListener(() => OnOptionSelected(option.text));
        }
    }

    void OnOptionSelected(string selectedOption) {
        ExecuteStep(selectedOption);
    }

    void ClearOptionButtons() {
        foreach (GameObject btn in _activeButtons) {
            Destroy(btn);
        }
        _activeButtons.Clear();
    }

    void AddTextToDisplay(string text, string hexColor) {
        storyDisplayText.text += $"\n\n<color={hexColor}>{text}</color>";
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    public void GoToMenu() {
        SaveGame();
        SceneManager.LoadScene("Saves"); 
    }

    public void SaveGame() {
        string json = JsonUtility.ToJson(new SaveData { history = _chatHistory });
        File.WriteAllText(_savePath, json);
    }

    public void LoadGame() {
        if (!File.Exists(_savePath)) return;
        string json = File.ReadAllText(_savePath);
        _chatHistory = JsonUtility.FromJson<SaveData>(json).history;

        storyDisplayText.text = "--- Adventure Resumed ---";
        ClearOptionButtons();

        ChatMessage lastMessage = null;

        foreach (var msg in _chatHistory) {
            if (msg.role == "user") AddTextToDisplay("<b>You:</b> " + msg.content, "#5fbff9");
            if (msg.role == "assistant") {
                lastMessage = msg;
                try {
                    StoryNode node = JsonUtility.FromJson<StoryNode>(CleanJson(msg.content));
                    string displayMessage = $"<b>DM:</b> {node.narrative}";
                    
                    if (node.options != null && node.options.Length > 0) {
                        displayMessage += "\n\n<b>Choices presented:</b>";
                        for (int i = 0; i < node.options.Length; i++) {
                            string rollSuffix = node.options[i].requiresRoll ? " 🎲" : "";
                            displayMessage += $"\n• {node.options[i].text}{rollSuffix}";
                        }
                    }
                    AddTextToDisplay(displayMessage, "#ffffff");
                } catch {
                    AddTextToDisplay("<b>DM:</b> " + msg.content, "#ffffff");
                }
            }
        }

        if (lastMessage != null) {
            try {
                StoryNode activeNode = JsonUtility.FromJson<StoryNode>(CleanJson(lastMessage.content));
                if (activeNode.options != null && activeNode.options.Length > 0) {
                    foreach (GameOption option in activeNode.options) {
                        CreateOptionButton(option);
                    }
                }
            } catch {
                Debug.LogError("Failed to restore choices on load.");
            }
        }
    }

    private async void StartNewGame() {
        string systemInstructions = 
            "You are a text adventure Dungeon Master. Follow these structural rules perfectly:\n\n" +
            "1. OUTPUT FORMAT: Respond ONLY with a valid JSON object. No explanations outside the JSON structure.\n\n" +
            "2. EXACT JSON BLUEPRINT STRUCT:\n" +
            "{\n" +
            "  \"narrative\": \"Your descriptive scene narration here (Max 3 sentences).\",\n" +
            "  \"options\": [\n" +
            "    { \"text\": \"Safe option description\", \"requiresRoll\": false },\n" +
            "    { \"text\": \"Risky or skilled action description\", \"requiresRoll\": true }\n" +
            "  ]\n" +
            "}\n\n" +
            "3. EVALUATING ROLLS: When the user passes an option containing a roll statement like '(You roll a d20 and get a X)', interpret low numbers (1-9) as failures or complications, and high numbers (10-20) as success. Progress the story instantly based on that value.\n" +
            "4. DYNAMIC ROLLS FLAG: Set 'requiresRoll' to true for individual options ONLY if they are inherently dangerous, complex, or require luck (e.g. pickpocketing, kicking down heavy iron doors, climbing walls). Conversations or simple looking around should be false.\n" +
            "5. OPTIONS RULES: Provide 2 to 3 options max. Options text must be very short (under 4 words).";

        _chatHistory.Add(new ChatMessage { role = "system", content = systemInstructions });
        await GetAiResponse("Start the game.");
    }

    private string CleanJson(string input) {
        int start = input.IndexOf('{');
        int end = input.LastIndexOf('}');
        return (start != -1 && end != -1) ? input.Substring(start, (end - start) + 1) : input;
    }
}