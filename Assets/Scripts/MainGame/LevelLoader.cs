// Applied on GamePanelUI
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    [Header("Reference")]
    public SoundManager soundManager;

    [Header("UI Elements")]
    public GameObject loginPanel;
    public GameObject homeUI;
    public GameObject gameUI;
    public GameObject roundOverUI;
    public AnimatedLineGraph profitGraph;
    public Transform tableGrid;
    public Transform questionTextParent;
    public Transform optionsParent;

    [Header("Text Elements")]
    public Text scenarioText;
    public Text companyText;
    public Text sectorText;
    public Text balanceText;
    public Text BankText;
    public Text timerText;
    public Text correctOptionText;
    public Text justificationText;
    public Text balanceTextRoundOver;
    public Text resultTextRoundOver;

    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject questionTextPrefab;
    public GameObject optionButtonPrefab;

    [Header("Points")]
    public int profit = 10;
    public int loss = -4;

    [Header("Text Styling")]
    public int fontSize = 20;
    public Color fontColor = Color.white;

    [Header("Timer Settings")]
    public float scenarioTimeLimit = 600f;
    private float remainingTime;
    private bool timerRunning = false;
    public bool isGameOver = false;

    [Header("Bank & Borrow")]
    public GameObject borrowPanel;
    public GameObject borrowOptionsPanel;
    public GameObject gameOverPanel;
    public Button quitButton;
    public Button[] borrowOptionButtons;
    public int[] borrowAmounts = { 10, 20, 30 };
    public int bankTotal = 100;
    private int bankTotalInStart;
    private bool isBorrowing = false;
    public AudioClip borrowSound;

    public List<LevelData> currentQuestionList;
    private int currentQuestionIndexInLevel = 0;
    private bool optionChosen;
    private bool allQuestionsCorrectInLevel = true;

    public GameLoader gameLoader;

    void Awake()
    {
        bankTotalInStart = bankTotal;
    }

    void Start()
    {
        gameUI.SetActive(true);
        homeUI.SetActive(false);
        roundOverUI.SetActive(false);

        remainingTime = scenarioTimeLimit;
        timerRunning = true;

        if (profitGraph != null)
        {
            profitGraph.ResetGraph();
            foreach (int p in GameLoader.profitBalance)
            {
                // profitGraph.AddLevelProfit(p);
                profitGraph.AddProfitPoint(p); // Add without animating
            }
        }
    }

    public int GetBankTotal()
    {
        return bankTotal;
    }

    void Update()
    {
        // if (roundOverUI.activeSelf) return; // Timer Paused during round over UI

        if (timerRunning)
        {
            remainingTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            if (remainingTime <= 0f)
            {
                timerRunning = false;
                Debug.Log("Time's up! Ending scenario.");
                EndScenarioDueToTimeout();
            }
        }
    }

    public void LoadLevel()
    {
        CanvasGroup gameCanvas = gameUI.GetComponent<CanvasGroup>();
        if (gameCanvas != null)
        {
            gameCanvas.alpha = 1f;
            gameCanvas.interactable = true;
            gameCanvas.blocksRaycasts = true;
        }

        if (GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
        {
            Debug.LogError("No more levels in this scenario.");
            return;
        }

        currentQuestionList = GameLoader.currentScenarioLevels[GameLoader.currentLevelIndex];
        currentQuestionIndexInLevel = 0;

        LoadQuestion();
    }

    public void LoadQuestion()
    {
        // Ensure gameUI is fully visible and interactable
        CanvasGroup gameCanvas = gameUI.GetComponent<CanvasGroup>();
        if (gameCanvas != null)
        {
            gameCanvas.alpha = 1f;
            gameCanvas.interactable = true;
            gameCanvas.blocksRaycasts = true;
        }

        ClearPreviousUI();
        optionChosen = false;

        if (currentQuestionIndexInLevel >= currentQuestionList.Count)
        {
            GameLoader.currentLevelIndex++;

            if (GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
            {
                GameLoader.currentScenarioIndex++;
                // GameLoader.currentLevelIndex = 0;

                if (GameLoader.currentScenarioIndex >= GameLoader.allScenarios.Count)
                {
                    Debug.Log("Game Complete! Show summary screen.");
                    return;
                }

                StartNewScenario();
                return;
            }

            if (GameLoader.streakByLevel)
            {
                if (allQuestionsCorrectInLevel)
                    GameLoader.currentStreak++;
                else
                    GameLoader.currentStreak = 0;

                allQuestionsCorrectInLevel = true;
            }

            GoToHomeScreen();
            return;
        }

        LevelData question = currentQuestionList[currentQuestionIndexInLevel];

        scenarioText.text = GameLoader.currentScenarioName;
        companyText.text = question.company;
        sectorText.text = question.sector;
        balanceText.text = GameLoader.playerBalance.ToString();
        justificationText.text = question.justification;
        BankText.text = bankTotal.ToString();
        correctOptionText.text = "Correct Option: " + question.correct;

        foreach (string statement in question.statements)
        {
            GameObject stmtObj = Instantiate(questionTextPrefab, questionTextParent);
            Text stmtText = stmtObj.GetComponent<Text>();
            stmtText.text = statement;
            stmtText.fontSize = fontSize;
            stmtText.color = fontColor;
            stmtObj.GetComponent<RectTransform>().localPosition = new Vector2(30, 74);
            stmtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(692, 230);
        }

        foreach (var row in question.table)
        {
            foreach (string cell in row.row)
            {
                GameObject cellObj = Instantiate(cellPrefab, tableGrid);
                cellObj.GetComponentInChildren<Text>().text = cell;
            }
        }

        foreach (string option in question.options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionsParent);
            btnObj.GetComponentInChildren<Text>().text = option;
            string selected = option;
            btnObj.GetComponent<Button>().onClick.AddListener(() => HandleOptionSelected(selected, question));
            soundManager.PlayButtonClick();
        }
    }

    public void TriggerBorrowPanel()
    {
        isBorrowing = true;
        borrowPanel.SetActive(true);
        borrowOptionsPanel.SetActive(false);
    }

    public void ShowBorrowOptions()
    {
        if (bankTotal == 0)
        {
            isGameOver = true;
            borrowPanel.SetActive(false);
            borrowOptionsPanel.SetActive(false);
            homeUI.SetActive(false);
            gameUI.SetActive(false);
            gameOverPanel.SetActive(true);
            Debug.Log("💀 Bank depleted. Game Over.");
            Time.timeScale = 0f;
        }
        else
        {
            borrowPanel.SetActive(false);
            borrowOptionsPanel.SetActive(true);

            // Set up borrow buttons
            for (int i = 0; i < borrowOptionButtons.Length; i++)
            {
                int amount = borrowAmounts[i];
                borrowOptionButtons[i].GetComponentInChildren<Text>().text = amount.ToString();
                borrowOptionButtons[i].onClick.RemoveAllListeners();
                borrowOptionButtons[i].onClick.AddListener(() => BorrowMoney(amount));
            }

            soundManager.PlayButtonClick();
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => QuitGame());
        }
    }

    void BorrowMoney(int amount)
    {
        int finalAmount = amount;

        // If bank has less than 10, give whatever is left
        if (bankTotal < 10)
        {
            finalAmount = bankTotal;
            bankTotal = 0;
            GameLoader.playerBalance += finalAmount;
            balanceText.text = GameLoader.playerBalance.ToString();
            BankText.text = bankTotal.ToString();
            borrowPanel.SetActive(false);
            if (bankTotal == 0 && GameLoader.playerBalance < 4)
            {
                // Show Game Over if player is still in debt and bank is empty
                isGameOver = true;
                borrowPanel.SetActive(false);
                borrowOptionsPanel.SetActive(false);
                homeUI.SetActive(false);
                gameUI.SetActive(false);
                gameOverPanel.SetActive(true);
                Time.timeScale = 0f;
                Debug.Log("💀 Player couldn't recover. Bank empty. Game Over.");
            }
            Debug.Log($"Bank has less than ₹10. Giving remaining ₹{finalAmount} to player.");
        }
        if (bankTotal >= amount)
        {
            bankTotal -= amount;
            AudioSource.PlayClipAtPoint(borrowSound, Camera.main.transform.position);
            GameLoader.playerBalance += amount;
            balanceText.text = GameLoader.playerBalance.ToString();
            BankText.text = bankTotal.ToString();

            if (GameLoader.playerBalance >= 0)
            {
                borrowPanel.SetActive(false);
                borrowOptionsPanel.SetActive(false);
                isBorrowing = false;
            }
        }
        else if (bankTotal == 0 && GameLoader.playerBalance < 0)
        {
            // Show Game Over if player is still in debt and bank is empty
            isGameOver = true;
            borrowPanel.SetActive(false);
            borrowOptionsPanel.SetActive(false);
            homeUI.SetActive(false);
            gameUI.SetActive(false);
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("💀 Player couldn't recover. Bank empty. Game Over.");
        }
    }

    void HandleOptionSelected(string selected, LevelData question)
    {
        // if (optionChosen) return;
        optionChosen = true;

        bool isCorrect = selected == question.correct;
        int points = isCorrect ? profit : loss;
        GameLoader.playerBalance += points;
        if (GameLoader.playerBalance < 0)
        {
            TriggerBorrowPanel();
            return; // Stop here — wait for player to borrow
        }

        if (GameLoader.streakByLevel)
        {
            if (!isCorrect)
                allQuestionsCorrectInLevel = false;
        }
        else
        {
            GameLoader.currentStreak = isCorrect ? GameLoader.currentStreak + 1 : 0;
        }

        // Disable and fade gameUI
        CanvasGroup gameCanvas = gameUI.GetComponent<CanvasGroup>();
        if (gameCanvas != null)
        {
            gameCanvas.interactable = false;
            gameCanvas.blocksRaycasts = false;
            gameCanvas.alpha = 0.5f; // Instantly fade to 0.5 (or use coroutine for smooth effect)
        }

        roundOverUI.SetActive(true);
        resultTextRoundOver.text = isCorrect ? "Correct!" : "Incorrect!";
        balanceTextRoundOver.text = "Balance: " + GameLoader.playerBalance;

        if (profitGraph != null)
        {
            GameLoader.profitBalance.Add(GameLoader.playerBalance);
            // AudioSource.PlayClipAtPoint(coinAddingSound, Camera.main.transform.position);
            profitGraph.AddProfitPoint(GameLoader.playerBalance);
        }
    }


    public void OnContinueButtonPressedInRoundOverPanel()
    {
        soundManager.PlayButtonClick();
        roundOverUI.SetActive(false);
        CanvasGroup gameCanvas = gameUI.GetComponent<CanvasGroup>();
        if (gameCanvas != null)
        {
            StartCoroutine(FadeCanvas(gameCanvas, 0.5f, 1f, 0.3f)); // Smooth fade back to full
        }

        currentQuestionIndexInLevel++;

        if (currentQuestionIndexInLevel < currentQuestionList.Count)
        {
            LoadQuestion();
        }
        else
        {
            if (GameLoader.streakByLevel)
            {
                if (allQuestionsCorrectInLevel)
                    GameLoader.currentStreak++;
                else
                    GameLoader.currentStreak = 0;

                allQuestionsCorrectInLevel = true;
            }

            // bool advanced = GameLoader.AdvanceToNextLevel();
            // Debug.Log("🔁 Level complete. AdvanceToNextLevel result: " + advanced);
            // GoToHomeScreen();

            GameLoader.currentLevelIndex++;

            if (GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
            {
                GameLoader.currentScenarioIndex++;
                GameLoader.currentLevelIndex = 0;

                if (GameLoader.currentScenarioIndex >= GameLoader.allScenarios.Count)
                {
                    Debug.Log("Game Complete!");
                    return;
                }

                StartNewScenario();
            }

            GoToHomeScreen();
        }
    }

    void GoToHomeScreen()
    {
        gameUI.SetActive(false);
        homeUI.SetActive(true);

        DynamicLevelUI uiScript = homeUI.GetComponentInChildren<DynamicLevelUI>();
        if (uiScript != null)
        {
            uiScript.RefreshButtons();
        }
        else
        {
            Debug.LogWarning("DynamicLevelUI script not found.");
        }
    }

    void ClearPreviousUI()
    {
        foreach (Transform child in tableGrid) Destroy(child.gameObject);
        foreach (Transform child in questionTextParent) Destroy(child.gameObject);
        foreach (Transform child in optionsParent) Destroy(child.gameObject);
    }

    void EndScenarioDueToTimeout()
    {
        GameLoader.currentScenarioIndex++;

        if (GameLoader.currentScenarioIndex >= GameLoader.allScenarios.Count)
        {
            Debug.Log("Game Complete due to timeout.");
            return;
        }
        if (profitGraph != null)
        {
            GameLoader.profitBalance.Add(GameLoader.playerBalance); // No change, just reflect paused state
            // profitGraph.AddLevelProfit(GameLoader.playerBalance);
            profitGraph.AddProfitPoint(GameLoader.playerBalance); // Add without animating
        }

        StartNewScenario();
        GoToHomeScreen();
    }

    public void StartNewScenario()
    {
        currentQuestionIndexInLevel = 0;
        // GameLoader.InitializeSelectedQuestions();
        GameLoader.currentLevelIndex = 0;

        remainingTime = scenarioTimeLimit;
        timerRunning = true;
        GameLoader.LoadNextScenario();
        Debug.Log("New scenario started. Timer reset.");
    }

    public void ResetGame()
    {
        Debug.Log("🔄 ResetGame called: resetting all game data...");

        // Reset GameLoader static variables
        GameLoader.currentScenarioIndex = 0;
        GameLoader.currentLevelIndex = 0;
        GameLoader.currentStreak = 0;
        GameLoader.playerBalance = 0;

        GameLoader.profitBalance.Clear();
        GameLoader.currentScenarioLevels.Clear();
        GameLoader.completedLevels.Clear();
        GameLoader.remainingScenarioNames.Clear();
        GameLoader.allScenarios.Clear();

        // Clear UI state
        DynamicLevelUI.completedLevels?.Clear();
        DynamicLevelUI.failedLevels?.Clear();
        DynamicLevelUI.timeoutLevels?.Clear();

        // Reload JSON and first scenario
        gameLoader.LoadJSON();
        GameLoader.LoadNextScenario();

        // Uodating Bank Amount
        bankTotal = bankTotalInStart;

        // Reset UI
        foreach (GameObject panel in GameObject.FindGameObjectsWithTag("UIPanel"))
            panel.SetActive(false);

        if (GameLoader.Instance != null)
        {
            GameLoader.Instance.homeUIPanel.SetActive(true);
            GameLoader.Instance.loginPanel?.SetActive(false);
        }

        // Reset Level Buttons
        DynamicLevelUI ui = GameObject.FindObjectOfType<DynamicLevelUI>();
        if (ui != null)
        {
            ui.RefreshButtons();
        }

        // Reset Profit Graph
        AnimatedLineGraph graph = GameObject.FindObjectOfType<AnimatedLineGraph>();
        if (graph != null)
        {
            graph.ResetGraph();
            graph.AddProfitPoint(0);
        }

        // Reset Bank
        LevelLoader levelLoader = GameObject.FindObjectOfType<LevelLoader>();
        if (levelLoader != null)
        {
            levelLoader.bankTotal = 100; // or your default
        }

        // Resume time if paused
        Time.timeScale = 1f;

        Debug.Log("✅ Game has been reset.");
    }


    IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        canvasGroup.alpha = startAlpha;

        while (time < duration)
        {
            float t = time / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            time += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;

        // Enable or disable interaction based on final alpha
        canvasGroup.interactable = endAlpha == 1f;
        canvasGroup.blocksRaycasts = endAlpha == 1f;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;  // Stop play mode in Editor
#else
        Application.Quit();  // Quit the app in build
#endif
    }
}
