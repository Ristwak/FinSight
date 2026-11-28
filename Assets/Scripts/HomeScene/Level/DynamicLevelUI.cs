using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
    [Range(0f, 1f)] public float crisisProbability = 0.15f;
    [Range(0f, 1f)] public float warProbability = 0f;

    public static HashSet<string> completedLevels = new();
    public static HashSet<string> failedLevels = new();
    public static HashSet<string> timeoutLevels = new();

    public SoundManager soundManager;

    private readonly List<Button> generatedButtons = new();
    private int currentStartIndex;
    private bool isTransitioning;

    public bool waitingForCrisis;

    private void Start()
    {
        if (homeUI != null) homeUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);

        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(() =>
            {
                PlayClick();
                PrevPage();
            });

        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(() =>
            {
                PlayClick();
                NextPage();
            });

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueButtonHandler);

        StartCoroutine(DelayedCreateButtons());
    }

    private IEnumerator DelayedCreateButtons()
    {
        yield return new WaitForEndOfFrame();
        RefreshButtons();
    }

    private void PlayClick()
    {
        if (soundManager != null)
            soundManager.PlayButtonClick();
    }

    private string GetLevelKey(int scenarioIndex, int levelIndex)
    {
        return $"{scenarioIndex}-{levelIndex}";
    }

    private void CreateButtons()
    {
        if (levelButtonPrefab == null || levelButtonContainer == null)
        {
            Debug.LogError("DynamicLevelUI: Button prefab or container is missing.");
            return;
        }

        int scenarioCount = GameLoader.allScenarios.Count;
        int levelsPerScenario = GameLoader.totalLevelsPerScenario;

        if (levelsPerScenario <= 0 || scenarioCount <= 0)
            return;

        int totalLevels = scenarioCount * levelsPerScenario;

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject obj = Instantiate(levelButtonPrefab, levelButtonContainer);

            Image image = obj.GetComponent<Image>();
            if (image != null && levelButtonsSprites != null &&
                levelButtonsSprites.Length > 0)
            {
                image.sprite = levelButtonsSprites[i % levelButtonsSprites.Length];
            }

            Button button = obj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("Level button prefab has no Button component.");
                continue;
            }

            Text label = obj.GetComponentInChildren<Text>();
            if (label != null)
                label.text = $"Level\n{i + 1}";

            int scenarioIndex = i / levelsPerScenario;
            int levelIndex = i % levelsPerScenario;

            string key = GetLevelKey(scenarioIndex, levelIndex);

            bool isCompleted = completedLevels.Contains(key);
            bool isFailed = failedLevels.Contains(key);
            bool isTimedOut = timeoutLevels.Contains(key);

            int currentFlatIndex =
                GameLoader.currentScenarioIndex * levelsPerScenario +
                GameLoader.currentLevelIndex;

            bool isCurrent = i == currentFlatIndex;
            bool isUnlocked = i <= currentFlatIndex;

            button.interactable = isCurrent && !isTransitioning;

            if (image != null)
            {
                if (isCurrent)
                    image.color = currentLevelColor;
                else if (isFailed)
                    image.color = failedLevelColor;
                else if (isTimedOut)
                    image.color = timeOutLevelColor;
                else if (isCompleted)
                    image.color = revisitedAndPassedColor;
                else if (isUnlocked)
                    image.color = passedLevelColor;
                else
                    image.color = lockedLevelColor;
            }

            Transform lockImage = obj.transform.Find("LockImage");
            if (lockImage != null)
                lockImage.gameObject.SetActive(!isUnlocked);

            if (label != null && (isCurrent || isCompleted))
                label.color = Color.white;

            button.onClick.AddListener(() =>
            {
                if (isCurrent && !isTransitioning)
                    PlayClick();
            });

            generatedButtons.Add(button);
        }
    }

    private void ShowPage(int startIndex)
    {
        if (startIndex < 0)
            startIndex = 0;

        for (int i = 0; i < generatedButtons.Count; i++)
        {
            if (generatedButtons[i] != null)
            {
                generatedButtons[i].gameObject.SetActive(
                    i >= startIndex && i < startIndex + pageSize
                );
            }
        }

        if (leftArrowButton != null)
            leftArrowButton.interactable = startIndex > 0;

        if (rightArrowButton != null)
            rightArrowButton.interactable =
                startIndex + pageSize < generatedButtons.Count;
    }

    private void NextPage()
    {
        if (currentStartIndex + pageSize < generatedButtons.Count)
        {
            currentStartIndex += pageSize;
            ShowPage(currentStartIndex);
        }
    }

    private void PrevPage()
    {
        if (currentStartIndex - pageSize >= 0)
        {
            currentStartIndex -= pageSize;
            ShowPage(currentStartIndex);
        }
    }

    private void ContinueButtonHandler()
    {
        if (isTransitioning || waitingForCrisis)
            return;

        if (GameLoader.currentScenarioLevels.Count == 0 ||
            GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
        {
            Debug.LogWarning("No active level is available.");
            return;
        }

        PlayClick();

        isTransitioning = true;
        SetNavigationInteractable(false);

        if (Random.value <= Mathf.Clamp01(crisisProbability))
        {
            bool triggerWar = Random.value <= Mathf.Clamp01(warProbability);

            if (triggerWar)
                StartWarEvent();
            else
                StartCrisisEvent();
        }
        else
        {
            StartSelectedLevel();
        }
    }

    private void StartCrisisEvent()
    {
        CrisisManager manager = FindObjectOfType<CrisisManager>();

        if (manager == null)
        {
            Debug.LogWarning("CrisisManager not found. Starting level normally.");
            StartSelectedLevel();
            return;
        }

        waitingForCrisis = true;

        if (soundManager != null)
            soundManager.StartCrisisLoop();

        manager.StartCrisis(() =>
        {
            waitingForCrisis = false;

            if (soundManager != null)
                soundManager.StopCrisisLoop();

            StartSelectedLevel();
        });
    }

    private void StartWarEvent()
    {
        WarManager manager = FindObjectOfType<WarManager>();

        if (manager == null)
        {
            Debug.LogWarning("WarManager not found. Starting level normally.");
            StartSelectedLevel();
            return;
        }

        waitingForCrisis = true;

        manager.StartWar(() =>
        {
            waitingForCrisis = false;
            StartSelectedLevel();
        });
    }

    private void StartSelectedLevel()
    {
        if (GameLoader.currentScenarioLevels.Count == 0 ||
            GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
        {
            isTransitioning = false;
            SetNavigationInteractable(true);
            Debug.LogWarning("Cannot start level: no current level data.");
            return;
        }

        if (homeUI != null) homeUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        LevelLoader loader = gameUI != null
            ? gameUI.GetComponent<LevelLoader>()
            : null;

        if (loader == null)
        {
            Debug.LogError("LevelLoader component is missing from GameUI.");
            isTransitioning = false;
            SetNavigationInteractable(true);
            return;
        }

        isTransitioning = false;
        loader.LoadLevel();
    }

    private void SetNavigationInteractable(bool value)
    {
        if (continueButton != null)
            continueButton.interactable = value;

        if (leftArrowButton != null)
            leftArrowButton.interactable = value && currentStartIndex > 0;

        if (rightArrowButton != null)
            rightArrowButton.interactable =
                value && currentStartIndex + pageSize < generatedButtons.Count;
    }

    public void RefreshButtons()
    {
        if (levelButtonContainer == null)
            return;

        foreach (Transform child in levelButtonContainer)
            Destroy(child.gameObject);

        generatedButtons.Clear();

        CreateButtons();

        int flatIndex =
            GameLoader.currentScenarioIndex * GameLoader.totalLevelsPerScenario +
            GameLoader.currentLevelIndex;

        currentStartIndex = (flatIndex / pageSize) * pageSize;

        ShowPage(currentStartIndex);
        SetNavigationInteractable(!isTransitioning);
    }
}