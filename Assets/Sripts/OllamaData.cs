using System;

[Serializable]
public class ChatMessage {
    public string role; // "system" (the rules) or "user" (your input)
    public string content;
}

[Serializable]
public class ChatRequest {
    public string model;
    public ChatMessage[] messages;
    public bool stream = false; // We want the whole answer at once [cite: 13, 300]
    public string format = "json"; // Tells the AI to speak in "code" we can read [cite: 54, 334]
}

[Serializable]
public class ChatResponse {
    public ChatMessage message; // The AI's actual reply [cite: 304]
}

//[cite_start]// This is the specific "Story" format we want from the AI [cite: 309]
[Serializable]
public class StoryNode {
    public string narrative;
    public string[] choices;
}