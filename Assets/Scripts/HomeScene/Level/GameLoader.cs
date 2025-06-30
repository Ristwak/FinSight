// Applied on GameManager
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject homeUIPanel;
    public static GameLoader Instance { get; private set; }
    public static Dictionary<string, List<LevelData>> allScenarios = new();
    public static List<List<LevelData>> currentScenarioLevels = new(); // 4 levels of 5 questions each
    public static List<string> remainingScenarioNames = new();
    public static string currentScenarioName = "";

    public static int currentScenarioIndex = 0;
    public static int currentLevelIndex;
    public static int currentStreak = 0;
    public static bool streakByLevel = true;

    public static int totalLevelsPerScenario = 4;
    public static int questionsPerLevel = 5;

    public static List<int> profitBalance = new();  // Track profit over time for graph
    public static HashSet<string> completedLevels = new();  // Prevent replays

    [Header("Borrowing Settings")]
    public static int playerBalance = 0;
    public static int totalBorrowLimit = 100;

    [TextArea(5, 20)]
    public string parsedJsonString;  // 👈 Viewable in Inspector

    public static JSONNode parsedJson { get; private set; } // Accessible elsewhere

    public LevelLoader levelLoader;

    void Awake()
    {
        Instance = this;


        loginPanel.SetActive(true);
        LoadJSON();
        LoadNextScenario();
    }

    public void LoadJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Scenarios");
        if (jsonFile == null)
        {
            Debug.LogError("JSON file not found in Resources/Data folder.");
            return;
        }

        parsedJson = JSON.Parse(jsonFile.text);
        Debug.Log("Printing parsedJson values" + parsedJson);
        allScenarios.Clear();
        remainingScenarioNames.Clear();

        foreach (KeyValuePair<string, JSONNode> scenario in parsedJson)
        {
            string key = scenario.Key;
            remainingScenarioNames.Add(key);

            var levelList = new List<LevelData>();
            JSONArray levelsArray = scenario.Value as JSONArray;
            if (levelsArray == null)
            {
                Debug.LogError($"❌ Scenario '{key}' is not a JSON array.");
                continue;
            }

            foreach (JSONNode levelNode in levelsArray)
            {
                JSONArray rawOptions = levelNode["options"].AsArray;
                string[] cleanedOptions = new string[rawOptions.Count];
                for (int i = 0; i < rawOptions.Count; i++)
                {
                    cleanedOptions[i] = RemoveSpecialCharacters(rawOptions[i]);
                }

                LevelData level = new LevelData
                {
                    company = levelNode["company"],
                    sector = levelNode["sector"],
                    statements = levelNode["statements"].AsArray.ToStringArray(),
                    options = cleanedOptions,
                    correct = levelNode["correct"],
                    justification = levelNode["justification"]
                };

                // Table parsing remains unchanged
                JSONArray tableArray = levelNode["table"].AsArray;
                level.table = new TableRow[tableArray.Count];
                for (int i = 0; i < tableArray.Count; i++)
                {
                    JSONArray rowArray = tableArray[i]["row"].AsArray;
                    TableRow row = new TableRow { row = new string[rowArray.Count] };
                    for (int k = 0; k < rowArray.Count; k++)
                    {
                        row.row[k] = rowArray[k];
                    }
                    level.table[i] = row;
                }

                levelList.Add(level);
            }


            allScenarios[key] = levelList;
            Debug.Log($"📦 Loaded scenario '{key}' with {levelList.Count} levels.");
        }

        Debug.Log("Scenarios loaded: " + allScenarios.Count);

    }

    public static string RemoveSpecialCharacters(string input)
    {
        // Remove [ ] ` '
        return input.Replace("[", "")
                    .Replace("]", "")
                    .Replace("`", "")
                    .Replace("'", "")
                    .Trim();
    }

    public static bool LoadNextScenario()
    {
        if (remainingScenarioNames.Count == 0)
        {
            Debug.Log("🎉 All scenarios completed!");
            return false;
        }

        // Pick a random scenario
        int randIndex = Random.Range(0, remainingScenarioNames.Count);
        currentScenarioName = remainingScenarioNames[randIndex];
        remainingScenarioNames.RemoveAt(randIndex);

        Debug.Log($"🧠 LoadNextScenario() called. Remaining: {remainingScenarioNames.Count}");
        Debug.Log($"➡️ Switching to scenario: {currentScenarioName}");

        currentLevelIndex = 0;
        currentScenarioLevels.Clear();

        List<LevelData> pool = allScenarios[currentScenarioName];
        if (pool.Count < totalLevelsPerScenario * questionsPerLevel)
        {
            Debug.LogError("❌ Not enough questions in scenario: " + currentScenarioName);
            return false;
        }

        // Shuffle and slice
        List<LevelData> shuffled = new List<LevelData>(pool);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int rnd = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[rnd]) = (shuffled[rnd], shuffled[i]);
        }

        for (int i = 0; i < totalLevelsPerScenario; i++)
        {
            List<LevelData> level = new();
            for (int j = 0; j < questionsPerLevel; j++)
            {
                level.Add(shuffled[i * questionsPerLevel + j]);
            }
            currentScenarioLevels.Add(level);
        }

        Debug.Log($"🚀 Loaded scenario: {currentScenarioName} ({allScenarios.Count - remainingScenarioNames.Count}/{allScenarios.Count})");
        Debug.Log("Remaining scenarios: " + string.Join(", ", remainingScenarioNames));
        return true;
    }

    public static List<LevelData> GetCurrentLevelQuestions()
    {
        if (currentLevelIndex < currentScenarioLevels.Count)
            return currentScenarioLevels[currentLevelIndex];
        else
            return new List<LevelData>();
    }
    
    // public void ResetStatics()
    // {
    //     currentScenarioIndex = 0;
    //     currentLevelIndex = 0;
    //     currentStreak = 0;
    //     playerBalance = 0;
    //     profitBalance.Clear();
    //     currentScenarioLevels.Clear();
    //     remainingScenarioNames.Clear();
    //     allScenarios.Clear();

    //     DynamicLevelUI.completedLevels?.Clear();
    //     DynamicLevelUI.failedLevels?.Clear();
    //     DynamicLevelUI.timeoutLevels?.Clear();
    // }

    public static void SoftResetGame()
    {
        currentScenarioIndex = 0;
        currentLevelIndex = 0;
        playerBalance = 0;
        currentStreak = 0;
        profitBalance.Clear();
        currentScenarioLevels.Clear();
        completedLevels.Clear();
        Debug.Log("Game soft reset to initial state.");
    }

    public static string GetScenarioNameByIndex(int index)
    {
        int i = 0;
        foreach (var kvp in allScenarios)
        {
            if (i == index)
                return kvp.Key;
            i++;
        }
        Debug.LogError("Scenario index out of range.");
        return "";
    }

}

// Helper for converting JSONArray to string[]
public static class JSONArrayExtensions
{
    public static string[] ToStringArray(this JSONArray array)
    {
        string[] result = new string[array.Count];
        for (int i = 0; i < array.Count; i++)
        {
            result[i] = array[i];
        }
        return result;
    }
}

[System.Serializable]
public class LevelData
{
    public string company;
    public string sector;
    public TableRow[] table;
    public string[] statements;
    public string[] options;
    public string correct;
    public string justification;
}

[System.Serializable]
public class TableRow
{
    public string[] row;
}
