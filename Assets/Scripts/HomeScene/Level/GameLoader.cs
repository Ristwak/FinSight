using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class GameLoader : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject homeUIPanel;

    public static GameLoader Instance { get; private set; }

    public static Dictionary<string, List<LevelData>> allScenarios = new();
    public static List<List<LevelData>> currentScenarioLevels = new();
    public static List<string> remainingScenarioNames = new();

    public static string currentScenarioName = "";

    public static int currentScenarioIndex = 0;
    public static int currentLevelIndex;
    public static int currentStreak = 0;
    public static bool streakByLevel = true;

    public static int totalLevelsPerScenario = 4;
    public static int questionsPerLevel = 5;

    public static List<int> profitBalance = new();
    public static HashSet<string> completedLevels = new();

    [Header("Borrowing Settings")]
    public static int playerBalance = 0;
    public static int totalBorrowLimit = 100;

    [TextArea(5, 20)]
    public string parsedJsonString;

    public static JSONNode parsedJson { get; private set; }

    public LevelLoader levelLoader;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (loginPanel != null)
            loginPanel.SetActive(true);
        else
            Debug.LogWarning("Login Panel is not assigned.");

        LoadJSON();

        if (allScenarios.Count > 0)
        {
            if (!LoadNextScenario())
                Debug.LogWarning("No valid scenarios available.");
        }
        else
        {
            Debug.LogError("No valid scenarios were loaded.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Scenarios");

        if (jsonFile == null)
        {
            Debug.LogError("JSON file not found at Resources/Data/Scenarios.");
            return;
        }

        try
        {
            parsedJsonString = jsonFile.text;
            parsedJson = JSON.Parse(jsonFile.text);

            if (parsedJson == null || !parsedJson.IsObject)
            {
                Debug.LogError("Invalid JSON: Root must be a JSON object.");
                return;
            }

            allScenarios.Clear();
            remainingScenarioNames.Clear();

            foreach (KeyValuePair<string, JSONNode> scenario in parsedJson)
            {
                string scenarioName = scenario.Key;
                JSONNode scenarioNode = scenario.Value;

                if (scenarioNode == null || !scenarioNode.IsArray)
                {
                    Debug.LogWarning(
                        $"Skipping scenario '{scenarioName}': Invalid scenario array."
                    );
                    continue;
                }

                List<LevelData> levelList = new List<LevelData>();

                foreach (JSONNode levelNode in scenarioNode.AsArray)
                {
                    if (!TryParseLevel(levelNode, out LevelData level))
                    {
                        Debug.LogWarning(
                            $"Skipping invalid question in scenario '{scenarioName}'."
                        );
                        continue;
                    }

                    levelList.Add(level);
                }

                int requiredQuestions = totalLevelsPerScenario * questionsPerLevel;

                if (levelList.Count < requiredQuestions)
                {
                    Debug.LogWarning(
                        $"Skipping scenario '{scenarioName}'. " +
                        $"Required: {requiredQuestions}, Available: {levelList.Count}."
                    );
                    continue;
                }

                allScenarios[scenarioName] = levelList;
                remainingScenarioNames.Add(scenarioName);

                Debug.Log(
                    $"Loaded scenario '{scenarioName}' with {levelList.Count} questions."
                );
            }

            Debug.Log($"Total valid scenarios loaded: {allScenarios.Count}");
        }
        catch (Exception exception)
        {
            allScenarios.Clear();
            remainingScenarioNames.Clear();

            Debug.LogError(
                $"Failed to load scenario JSON: {exception.Message}"
            );
        }
    }

    private bool TryParseLevel(JSONNode levelNode, out LevelData level)
    {
        level = null;

        if (levelNode == null || !levelNode.IsObject)
            return false;

        JSONNode optionsNode = levelNode["options"];
        JSONNode statementsNode = levelNode["statements"];
        JSONNode tableNode = levelNode["table"];

        if (optionsNode == null || !optionsNode.IsArray ||
            statementsNode == null || !statementsNode.IsArray)
        {
            return false;
        }

        if (optionsNode.AsArray.Count == 0 ||
            statementsNode.AsArray.Count == 0)
        {
            return false;
        }

        string correct = levelNode["correct"].Value;

        if (string.IsNullOrWhiteSpace(correct))
            return false;

        string[] options = new string[optionsNode.AsArray.Count];

        for (int i = 0; i < options.Length; i++)
        {
            options[i] = RemoveSpecialCharacters(
                optionsNode.AsArray[i].Value
            );
        }

        string[] statements = new string[statementsNode.AsArray.Count];

        for (int i = 0; i < statements.Length; i++)
        {
            statements[i] = statementsNode.AsArray[i].Value;
        }

        TableRow[] tableRows = new TableRow[0];

        if (tableNode != null && tableNode.IsArray)
        {
            tableRows = new TableRow[tableNode.AsArray.Count];

            for (int i = 0; i < tableRows.Length; i++)
            {
                JSONNode rowNode = tableNode.AsArray[i]["row"];

                if (rowNode == null || !rowNode.IsArray)
                    return false;

                string[] rowData = new string[rowNode.AsArray.Count];

                for (int j = 0; j < rowData.Length; j++)
                {
                    rowData[j] = rowNode.AsArray[j].Value;
                }

                tableRows[i] = new TableRow
                {
                    row = rowData
                };
            }
        }

        level = new LevelData
        {
            company = levelNode["company"].Value,
            sector = levelNode["sector"].Value,
            statements = statements,
            options = options,
            correct = correct,
            justification = levelNode["justification"].Value,
            table = tableRows
        };

        return true;
    }

    public static string RemoveSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.Replace("[", "")
                    .Replace("]", "")
                    .Replace("`", "")
                    .Replace("'", "")
                    .Trim();
    }

    public static bool LoadNextScenario()
    {
        currentScenarioLevels.Clear();
        currentLevelIndex = 0;

        if (totalLevelsPerScenario <= 0 || questionsPerLevel <= 0)
        {
            Debug.LogError("Invalid scenario configuration.");
            currentScenarioName = string.Empty;
            return false;
        }

        int requiredQuestions = totalLevelsPerScenario * questionsPerLevel;

        while (remainingScenarioNames.Count > 0)
        {
            int randomIndex = Random.Range(0, remainingScenarioNames.Count);
            string selectedScenario = remainingScenarioNames[randomIndex];

            if (!allScenarios.TryGetValue(selectedScenario, out List<LevelData> pool) ||
                pool == null || pool.Count < requiredQuestions)
            {
                Debug.LogWarning($"Skipping invalid scenario: {selectedScenario}");
                remainingScenarioNames.RemoveAt(randomIndex);
                continue;
            }

            remainingScenarioNames.RemoveAt(randomIndex);
            currentScenarioName = selectedScenario;

            List<LevelData> shuffled = new List<LevelData>(pool);

            // Fisher-Yates shuffle.
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int randomIndexToSwap = Random.Range(0, i + 1);

                (shuffled[i], shuffled[randomIndexToSwap]) =
                    (shuffled[randomIndexToSwap], shuffled[i]);
            }

            for (int i = 0; i < totalLevelsPerScenario; i++)
            {
                List<LevelData> level = new List<LevelData>();

                for (int j = 0; j < questionsPerLevel; j++)
                {
                    int questionIndex = i * questionsPerLevel + j;
                    level.Add(shuffled[questionIndex]);
                }

                currentScenarioLevels.Add(level);
            }

            Debug.Log(
                $"Loaded scenario: {currentScenarioName}. " +
                $"Remaining: {remainingScenarioNames.Count}"
            );

            return true;
        }

        currentScenarioName = string.Empty;
        Debug.Log("All valid scenarios have been completed.");

        return false;
    }

    public static List<LevelData> GetCurrentLevelQuestions()
    {
        if (currentScenarioLevels == null ||
            currentLevelIndex < 0 ||
            currentLevelIndex >= currentScenarioLevels.Count)
        {
            Debug.LogWarning("Current level index is invalid.");
            return new List<LevelData>();
        }

        return currentScenarioLevels[currentLevelIndex];
    }

    public static void SoftResetGame()
    {
        currentScenarioIndex = 0;
        currentLevelIndex = 0;
        currentScenarioName = string.Empty;

        playerBalance = 0;
        currentStreak = 0;

        profitBalance.Clear();
        currentScenarioLevels.Clear();
        completedLevels.Clear();

        // Restore all valid scenarios for a fresh run.
        remainingScenarioNames.Clear();

        foreach (string scenarioName in allScenarios.Keys)
        {
            remainingScenarioNames.Add(scenarioName);
        }

        Debug.Log("Game soft reset to initial state.");
    }

    public static string GetScenarioNameByIndex(int index)
    {
        if (index < 0 || index >= allScenarios.Count)
        {
            Debug.LogError("Scenario index out of range.");
            return string.Empty;
        }

        int currentIndex = 0;

        foreach (string scenarioName in allScenarios.Keys)
        {
            if (currentIndex == index)
                return scenarioName;

            currentIndex++;
        }

        return string.Empty;
    }
}

public static class JSONArrayExtensions
{
    public static string[] ToStringArray(this JSONArray array)
    {
        if (array == null)
            return new string[0];

        string[] result = new string[array.Count];

        for (int i = 0; i < array.Count; i++)
        {
            result[i] = array[i].Value;
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