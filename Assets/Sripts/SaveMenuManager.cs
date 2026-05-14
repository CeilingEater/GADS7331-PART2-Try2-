using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // <-- FIX: This prevents the SceneManager error
using System.IO;
using TMPro;

public class SaveMenuManager : MonoBehaviour {
    [Header("Settings")]
    public GameObject saveButtonPrefab; // The 'template' button from your project folder
    public Transform contentPanel;      // The 'Content' object inside your Scroll View

    void Start() {
        RefreshSaveList();
    }

    public void RefreshSaveList() {
        // 1. Delete old buttons so we don't double up
        foreach (Transform child in contentPanel) Destroy(child.gameObject);

        // 2. Check the folder for saves
        if (!Directory.Exists(Application.persistentDataPath)) return;
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");

        // 3. For every file, make a new button
        foreach (string filePath in files) {
            string fileName = Path.GetFileName(filePath);
            
            // Create the clone
            GameObject newBtn = Instantiate(saveButtonPrefab, contentPanel);
            
            // Set the button text to the filename
            newBtn.GetComponentInChildren<TMP_Text>().text = fileName;

            // Make the button load that specific file when clicked
            newBtn.GetComponent<Button>().onClick.AddListener(() => LoadAdventure(fileName));
        }
    }

    public void CreateNewAdventure() {
        // Creates a name based on current time (e.g., Adventure_20260514)
        string newFileName = "Adv_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
        GameDataHandler.CurrentSaveFile = newFileName;
        SceneManager.LoadScene("AdventureScene"); // Make sure this name matches your scene!
    }

    void LoadAdventure(string fileName) {
        GameDataHandler.CurrentSaveFile = fileName;
        SceneManager.LoadScene("AdventureScene");
    }
}