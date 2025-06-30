// Applied on LevelButtonContainer in HomeUIPanel
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class DynamicLevelUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject homeUI;
    public GameObject gameUI;
    public GameObject levelButtonPrefab;
    public Transform levelButtonContainer;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button continueButton;

    [Header("Level Buttons")]
    public Sprite[] levelButtonsSprites;

    [Header("Colors")]
    public Color failedLevelColor = Color.red;
    public Color passedLevelColor = Color.green;
    public Color currentLevelColor = Color.white;
    public Color lockedLevelColor = Color.gray;
    public Color timeOutLevelColor = Color.red;
    public Color revisitedAndPassedColor = new Color(0.5f, 0.8f, 1f);

    [Header("Page Settings")]
    public const int pageSize = 7;

    [Header("Game Data")]
    public float crisisProbability = 0.15f;
    public float warProbability = 0f;

    public static HashSet<string> completedLevels = new();
    public static HashSet<string> failedLevels = new();
    public static HashSet<string> timeoutLevels = new();

    public SoundManager soundManager;
    private List<Button> generatedButtons = new();
    private int currentStartIndex = 0;
    public bool waitingForCrisis = false;

    void Start()
    {
        homeUI.SetActive(true);
        gameUI.SetActive(false);

        leftArrowButton.onClick.AddListener(() =>
        {
            soundManager.PlayButtonClick();
            PrevPage();
        });

        rightArrowButton.onClick.AddListener(() =>
        {
            soundManager.PlayButtonClick();
            NextPage();
        });

        continueButton.onClick.AddListener(() =>
        {
            soundManager.PlayButtonClick();
            ContinueButtonHandler();
        });
        StartCoroutine(DelayedCreateButtons());
    }

    IEnumerator DelayedCreateButtons()
    {
        yield return new WaitForEndOfFrame();
        CreateButtons();
        ShowPage(currentStartIndex);
    }

    string GetLevelKey(int scenarioIndex, int levelIndex)
    {
        return $"{scenarioIndex}-{levelIndex}";
    }

    void CreateButtons()
    {
        int totalScenarios = GameLoader.allScenarios.Count;
        int totalLevelsPerScenario = GameLoader.totalLevelsPerScenario;
        int totalLevels = totalScenarios * totalLevelsPerScenario;

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            Image btnImage = btnObj.GetComponent<Image>();
            if (btnImage != null)
                btnImage.sprite = levelButtonsSprites[i % levelButtonsSprites.Length];

            Button btn = btnObj.GetComponent<Button>();
            Text label = btnObj.GetComponentInChildren<Text>();
            if (label != null) label.text = $"Level \n{i + 1}";

            // 👇 Add this line to find the lock image (assuming it's named "LockImage")
            Transform lockImageTransform = btnObj.transform.Find("LockImage");
            bool hasLockImage = lockImageTransform != null;

            int index = i;

            // Compute scenario and level index
            int scenarioIndex = index / totalLevelsPerScenario;
            int levelIndex = index % totalLevelsPerScenario;

            string key = GetLevelKey(scenarioIndex, levelIndex);
            bool isCompleted = completedLevels.Contains(key);
            bool isUnlocked = scenarioIndex < GameLoader.currentScenarioIndex ||
                            (scenarioIndex == GameLoader.currentScenarioIndex && levelIndex <= GameLoader.currentLevelIndex);


            // If completed, it should no longer be interactable
            // Only the current level should be interactable
            int flatIndex = scenarioIndex * GameLoader.totalLevelsPerScenario + levelIndex;
            int currentFlatIndex = GameLoader.currentScenarioIndex * GameLoader.totalLevelsPerScenario + GameLoader.currentLevelIndex;

            bool isCurrent = flatIndex == currentFlatIndex;

            btn.interactable = isCurrent;


            if (btnImage != null)
            {
                // Current Active Level
                if (scenarioIndex == GameLoader.currentScenarioIndex && levelIndex == GameLoader.currentLevelIndex)
                {
                    btnImage.color = currentLevelColor;
                    Color labelColor = Color.white;
                    label.color = labelColor;
                }
                // Failed Levels
                else if (failedLevels.Contains(key) || timeoutLevels.Contains(key))
                {
                    btnImage.color = failedLevelColor;
                    Color labelColor = Color.white;
                    label.color = labelColor;
                }
                // Revist and Complete Levels
                else if (completedLevels.Contains(key))
                {
                    btnImage.color = revisitedAndPassedColor;
                    Color labelColor = Color.white;
                    label.color = labelColor;
                }
                // Passed Levels
                else if (isUnlocked)
                {
                    btnImage.color = passedLevelColor;
                    Color labelColor = Color.white;
                    label.color = labelColor;
                }
            }
            // 👇 Lock logic
            if (hasLockImage)
            {
                lockImageTransform.gameObject.SetActive(!isUnlocked);
            }


            btn.onClick.AddListener(() =>
            {
                if (scenarioIndex == GameLoader.currentScenarioIndex && levelIndex == GameLoader.currentLevelIndex)
                {
                    soundManager.PlayButtonClick();
                    // StartSelectedLevel();
                }
            });

            generatedButtons.Add(btn);
        }
    }

    void ShowPage(int startIndex)
    {
        for (int i = 0; i < generatedButtons.Count; i++)
        {
            generatedButtons[i].gameObject.SetActive(i >= startIndex && i < startIndex + pageSize);
        }

        leftArrowButton.interactable = startIndex > 0;
        rightArrowButton.interactable = startIndex + pageSize < generatedButtons.Count;
    }

    void NextPage()
    {
        if (currentStartIndex + pageSize < generatedButtons.Count)
        {
            currentStartIndex += pageSize;
            ShowPage(currentStartIndex);
        }
    }

    void PrevPage()
    {
        if (currentStartIndex - pageSize >= 0)
        {
            currentStartIndex -= pageSize;
            ShowPage(currentStartIndex);
        }
    }

    void ContinueButtonHandler()
    {
        soundManager.PlayButtonClick();
        if (waitingForCrisis)
        {
            Debug.LogWarning("Already waiting for a crisis or war event.");
        }


        float rand = Random.value;

        if (rand <= crisisProbability)
        {
            waitingForCrisis = true;

            bool triggerCrisis = Random.value <= warProbability;

            if (triggerCrisis)
            {
                Debug.Log("Crisis triggered!");
                var crisisManager = FindObjectOfType<CrisisManager>();
                if (crisisManager != null)
                {
                    soundManager.StartCrisisLoop();
                    crisisManager.StartCrisis(() =>
                    {
                        waitingForCrisis = false;
                        soundManager.StopCrisisLoop();

                        // 💰 Update the graph and balance
                        AnimatedLineGraph graph = FindObjectOfType<AnimatedLineGraph>();
                        if (graph != null)
                        {
                            graph.AddProfitPoint(GameLoader.playerBalance); // assumes playerProfit already updated by crisis
                        }

                        // 🧾 Show borrow panel if needed
                        LevelLoader levelLoader = FindObjectOfType<LevelLoader>();
                        if (levelLoader != null && GameLoader.playerBalance < 0)
                        {
                            levelLoader.TriggerBorrowPanel(); // ⬅️ Make sure this method exists in your LevelLoader
                        }

                        homeUI.SetActive(true);
                        gameUI.SetActive(false);
                    });
                }
                else
                {
                    Debug.LogWarning("CrisisManager not found.");
                    waitingForCrisis = false;
                    StartSelectedLevel();
                    RefreshButtons();
                }
            }
            else
            {
                Debug.Log("War triggered!");
                var warManager = FindObjectOfType<WarManager>();
                if (warManager != null)
                {
                    warManager.StartWar(() =>
                    {
                        waitingForCrisis = false;
                        StartSelectedLevel();
                    });
                }
                else
                {
                    Debug.LogWarning("WarManager not found.");
                    waitingForCrisis = false;
                    StartSelectedLevel();
                }
            }
        }
        else
        {
            StartSelectedLevel(); // No event
        }
    }

    void StartSelectedLevel()
    {
        Debug.Log($"Starting Scenario {GameLoader.currentScenarioIndex + 1}, Level {GameLoader.currentLevelIndex + 1}");
        homeUI.SetActive(false);
        gameUI.SetActive(true);

        LevelLoader loader = gameUI.GetComponent<LevelLoader>();
        if (loader != null)
        {
            loader.LoadLevel();
        }
        else
        {
            Debug.LogError("LevelLoader script missing on GameUI object.");
        }
    }

    public void RefreshButtons()
    {
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }

        generatedButtons.Clear();
        CreateButtons();

        int flatIndex = (GameLoader.currentScenarioIndex * GameLoader.totalLevelsPerScenario) + GameLoader.currentLevelIndex;
        currentStartIndex = (flatIndex / pageSize) * pageSize;
        ShowPage(currentStartIndex);
    }
}
