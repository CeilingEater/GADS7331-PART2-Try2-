using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient {
    // Ensure there are no accidental spaces in this URL
    private string _url = "http://127.0.0.1:11434/api/chat";

    public async Task<string> GetAiReply(ChatRequest requestData) {
        string jsonPayload = JsonUtility.ToJson(requestData);
        using var request = new UnityWebRequest(_url, "POST");
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var operation = request.SendWebRequest();
        
        // Wait for the request to finish
        while (!operation.isDone) await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success) {
            var response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
            return response.message.content;
        } else {
            // This will tell us exactly what went wrong (e.g., Timeout, 404, etc.)
            Debug.LogError($"Ollama Connection Failed: {request.error}");
            Debug.LogError($"Check if Ollama is running at {_url}");
            return null;
        }
    }
}