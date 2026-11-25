using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
    private bool timerRunning;
    public bool isGameOver;

    [Header("Bank & Borrow")]
    public GameObject borrowPanel;
    public GameObject borrowOptionsPanel;
    public GameObject gameOverPanel;
    public Button quitButton;
    public Button[] borrowOptionButtons;
    public int[] borrowAmounts = { 10, 20, 30 };
    public int bankTotal = 100;
    private int bankTotalInStart;
    private bool isBorrowing;
    public AudioClip borrowSound;

    public List<LevelData> currentQuestionList;
    private int currentQuestionIndexInLevel;
    private bool optionChosen;
    private bool allQuestionsCorrectInLevel = true;
    private bool hasPendingAnswer;
    private bool pendingAnswerCorrect;

    public Action onBorrowCompleted;

    public GameLoader gameLoader;

    private void Awake()
    {
        bankTotalInStart = bankTotal;
    }

    private void Start()
    {
        if (gameUI != null) gameUI.SetActive(true);
        if (homeUI != null) homeUI.SetActive(false);
        if (roundOverUI != null) roundOverUI.SetActive(false);

        remainingTime = Mathf.Max(0f, scenarioTimeLimit);
        timerRunning = remainingTime > 0f;

        if (profitGraph != null)
        {
            profitGraph.ResetGraph();

            foreach (int value in GameLoader.profitBalance)
                profitGraph.AddProfitPoint(value);
        }

        UpdateBalanceUI();
    }

    private void Update()
    {
        if (!timerRunning || isGameOver || isBorrowing)
            return;

        remainingTime -= Time.deltaTime;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(Mathf.Max(0f, remainingTime) / 60f);
            int seconds = Mathf.FloorToInt(Mathf.Max(0f, remainingTime) % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (remainingTime <= 0f)
        {
            timerRunning = false;
            EndScenarioDueToTimeout();
        }
    }

    public int GetBankTotal()
    {
        return bankTotal;
    }

    public void LoadLevel()
    {
        if (isGameOver)
            return;

        if (gameUI == null)
        {
            Debug.LogError("GameUI reference is missing.");
            return;
        }

        CanvasGroup canvas = gameUI.GetComponent<CanvasGroup>();
        SetCanvasState(canvas, 1f, true);

        if (GameLoader.currentLevelIndex < 0 ||
            GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
        {
            Debug.LogError("Current level index is invalid.");
            return;
        }

        currentQuestionList =
            GameLoader.GetCurrentLevelQuestions();

        if (currentQuestionList == null || currentQuestionList.Count == 0)
        {
            Debug.LogError("No questions available for this level.");
            return;
        }

        currentQuestionIndexInLevel = 0;
        allQuestionsCorrectInLevel = true;
        LoadQuestion();
    }

    public void LoadQuestion()
    {
        if (isGameOver || isBorrowing)
            return;

        if (gameUI == null || currentQuestionList == null)
            return;

        CanvasGroup canvas = gameUI.GetComponent<CanvasGroup>();
        SetCanvasState(canvas, 1f, true);

        ClearPreviousUI();
        optionChosen = false;

        if (currentQuestionIndexInLevel >= currentQuestionList.Count)
        {
            CompleteCurrentLevel();
            return;
        }

        LevelData question = currentQuestionList[currentQuestionIndexInLevel];

        if (question == null)
        {
            Debug.LogError("Question data is missing.");
            return;
        }

        if (scenarioText != null)
            scenarioText.text = GameLoader.currentScenarioName;

        if (companyText != null)
            companyText.text = question.company;

        if (sectorText != null)
            sectorText.text = question.sector;

        if (justificationText != null)
            justificationText.text = question.justification;

        if (correctOptionText != null)
            correctOptionText.text = "Correct Option: " + question.correct;

        UpdateBalanceUI();

        if (question.statements != null && questionTextParent != null)
        {
            foreach (string statement in question.statements)
            {
                if (questionTextPrefab == null) break;

                GameObject obj = Instantiate(questionTextPrefab, questionTextParent);
                Text text = obj.GetComponent<Text>();

                if (text != null)
                {
                    text.text = statement;
                    text.fontSize = fontSize;
                    text.color = fontColor;
                }

                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localPosition = new Vector2(30, 74);
                    rect.sizeDelta = new Vector2(692, 230);
                }
            }
        }

        if (question.table != null && tableGrid != null)
        {
            foreach (TableRow row in question.table)
            {
                if (row == null || row.row == null) continue;

                foreach (string cell in row.row)
                {
                    if (cellPrefab == null) break;

                    GameObject obj = Instantiate(cellPrefab, tableGrid);
                    Text text = obj.GetComponentInChildren<Text>();

                    if (text != null)
                        text.text = cell;
                }
            }
        }

        if (question.options == null || question.options.Length == 0 ||
            optionsParent == null || optionButtonPrefab == null)
        {
            Debug.LogError("Question options or option UI references are missing.");
            return;
        }

        foreach (string option in question.options)
        {
            GameObject obj = Instantiate(optionButtonPrefab, optionsParent);

            Text text = obj.GetComponentInChildren<Text>();
            if (text != null)
                text.text = option;

            Button button = obj.GetComponent<Button>();
            if (button == null) continue;

            string selected = option;
            button.onClick.AddListener(
                () => HandleOptionSelected(selected, question)
            );
        }
    }

    private void HandleOptionSelected(string selected, LevelData question)
    {
        if (optionChosen || isGameOver || isBorrowing)
            return;

        optionChosen = true;

        if (optionsParent != null)
        {
            foreach (Transform child in optionsParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }

        bool isCorrect = selected == question.correct;

        GameLoader.playerBalance += isCorrect ? profit : loss;

        pendingAnswerCorrect = isCorrect;
        hasPendingAnswer = true;

        if (GameLoader.playerBalance < 0)
        {
            TriggerBorrowPanel();
            return;
        }

        CompletePendingAnswer();
    }

    private void CompletePendingAnswer()
    {
        if (!hasPendingAnswer || isGameOver)
            return;

        bool isCorrect = pendingAnswerCorrect;

        hasPendingAnswer = false;

        UpdateStreak(isCorrect);

        CanvasGroup canvas = gameUI != null
            ? gameUI.GetComponent<CanvasGroup>()
            : null;

        SetCanvasState(canvas, 0.5f, false);

        if (roundOverUI != null)
            roundOverUI.SetActive(true);

        if (resultTextRoundOver != null)
            resultTextRoundOver.text = isCorrect ? "Correct!" : "Incorrect!";

        if (balanceTextRoundOver != null)
            balanceTextRoundOver.text = "Balance: " + GameLoader.playerBalance;

        RecordProfitPoint();
    }

    private void UpdateStreak(bool isCorrect)
    {
        if (GameLoader.streakByLevel)
        {
            if (!isCorrect)
                allQuestionsCorrectInLevel = false;
        }
        else
        {
            GameLoader.currentStreak =
                isCorrect ? GameLoader.currentStreak + 1 : 0;
        }
    }

    private void CompleteCurrentLevel()
    {
        if (GameLoader.streakByLevel)
        {
            GameLoader.currentStreak =
                allQuestionsCorrectInLevel
                    ? GameLoader.currentStreak + 1
                    : 0;
        }

        allQuestionsCorrectInLevel = true;

        GameLoader.currentLevelIndex++;

        if (GameLoader.currentLevelIndex >= GameLoader.currentScenarioLevels.Count)
        {
            GameLoader.currentScenarioIndex++;

            if (!StartNewScenario())
            {
                EndGame();
                return;
            }
        }

        GoToHomeScreen();
    }

    public void OnContinueButtonPressedInRoundOverPanel()
    {
        if (isGameOver || isBorrowing)
            return;

        if (soundManager != null)
            soundManager.PlayButtonClick();

        if (roundOverUI != null)
            roundOverUI.SetActive(false);

        CanvasGroup canvas = gameUI != null
            ? gameUI.GetComponent<CanvasGroup>()
            : null;

        SetCanvasState(canvas, 1f, true);

        currentQuestionIndexInLevel++;

        if (currentQuestionIndexInLevel < currentQuestionList.Count)
        {
            LoadQuestion();
        }
        else
        {
            CompleteCurrentLevel();
        }
    }

    public void TriggerBorrowPanel()
    {
        if (isGameOver) return;

        isBorrowing = true;
        timerRunning = false;

        if (borrowPanel != null)
            borrowPanel.SetActive(true);

        if (borrowOptionsPanel != null)
            borrowOptionsPanel.SetActive(false);
    }

    public void ShowBorrowOptions()
    {
        if (!isBorrowing || isGameOver)
            return;

        if (bankTotal <= 0)
        {
            HandleGameOver();
            return;
        }

        if (borrowPanel != null)
            borrowPanel.SetActive(false);

        if (borrowOptionsPanel != null)
            borrowOptionsPanel.SetActive(true);

        if (borrowOptionButtons != null)
        {
            for (int i = 0; i < borrowOptionButtons.Length; i++)
            {
                if (borrowOptionButtons[i] == null)
                    continue;

                if (borrowAmounts == null || i >= borrowAmounts.Length)
                {
                    borrowOptionButtons[i].interactable = false;
                    continue;
                }

                int amount = borrowAmounts[i];

                Text label = borrowOptionButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.text = Mathf.Min(amount, bankTotal).ToString();

                borrowOptionButtons[i].onClick.RemoveAllListeners();
                borrowOptionButtons[i].onClick.AddListener(
                    () => BorrowMoney(amount)
                );

                borrowOptionButtons[i].interactable = amount > 0;
            }
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        if (soundManager != null)
            soundManager.PlayButtonClick();
    }

    private void BorrowMoney(int amount)
    {
        if (!isBorrowing || isGameOver || amount <= 0 || bankTotal <= 0)
            return;

        int finalAmount = Mathf.Min(amount, bankTotal);

        bankTotal -= finalAmount;
        GameLoader.playerBalance += finalAmount;

        if (borrowSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(borrowSound, Camera.main.transform.position);

        UpdateBalanceUI();

        if (GameLoader.playerBalance >= 0)
        {
            isBorrowing = false;
            timerRunning = remainingTime > 0f;

            if (borrowPanel != null)
                borrowPanel.SetActive(false);

            if (borrowOptionsPanel != null)
                borrowOptionsPanel.SetActive(false);

            if (hasPendingAnswer)
            {
                CompletePendingAnswer();
            }
            else
            {
                CanvasGroup canvas = gameUI != null
                    ? gameUI.GetComponent<CanvasGroup>()
                    : null;

                SetCanvasState(canvas, 1f, true);

                Action callback = onBorrowCompleted;
                onBorrowCompleted = null;
                callback?.Invoke();
            }

            return;
        }

        if (bankTotal <= 0)
            HandleGameOver();
    }

    private void HandleGameOver()
    {
        isGameOver = true;
        hasPendingAnswer = false;
        onBorrowCompleted = null;
        isBorrowing = false;
        timerRunning = false;

        if (borrowPanel != null) borrowPanel.SetActive(false);
        if (borrowOptionsPanel != null) borrowOptionsPanel.SetActive(false);
        if (homeUI != null) homeUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
        Debug.Log("Game Over: Bank depleted while player remains in debt.");
    }

    private void UpdateBalanceUI()
    {
        if (balanceText != null)
            balanceText.text = GameLoader.playerBalance.ToString();

        if (BankText != null)
            BankText.text = bankTotal.ToString();
    }

    private void RecordProfitPoint()
    {
        GameLoader.profitBalance.Add(GameLoader.playerBalance);

        if (profitGraph != null)
            profitGraph.AddProfitPoint(GameLoader.playerBalance);
    }

    private void GoToHomeScreen()
    {
        if (gameUI != null) gameUI.SetActive(false);
        if (homeUI != null) homeUI.SetActive(true);

        DynamicLevelUI ui = homeUI != null
            ? homeUI.GetComponentInChildren<DynamicLevelUI>()
            : null;

        if (ui != null)
            ui.RefreshButtons();
    }

    private void ClearPreviousUI()
    {
        ClearChildren(tableGrid);
        ClearChildren(questionTextParent);
        ClearChildren(optionsParent);
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }

    private bool StartNewScenario()
    {
        currentQuestionIndexInLevel = 0;
        GameLoader.currentLevelIndex = 0;

        if (!GameLoader.LoadNextScenario())
            return false;

        remainingTime = Mathf.Max(0f, scenarioTimeLimit);
        timerRunning = remainingTime > 0f;

        Debug.Log("New scenario started.");
        return true;
    }

    private void EndScenarioDueToTimeout()
    {
        if (isGameOver) return;

        RecordProfitPoint();

        GameLoader.currentScenarioIndex++;

        if (!StartNewScenario())
        {
            EndGame();
            return;
        }

        GoToHomeScreen();
    }

    private void EndGame()
    {
        timerRunning = false;
        isGameOver = true;

        if (gameUI != null) gameUI.SetActive(false);
        if (homeUI != null) homeUI.SetActive(false);

        Debug.Log("All scenarios completed. Game finished.");
    }

    public void ResetGame()
    {
        Debug.Log("Resetting game...");

        Time.timeScale = 1f;

        isGameOver = false;
        isBorrowing = false;
        timerRunning = false;
        optionChosen = false;
        allQuestionsCorrectInLevel = true;
        hasPendingAnswer = false;
        pendingAnswerCorrect = false;
        onBorrowCompleted = null;

        currentQuestionIndexInLevel = 0;
        currentQuestionList = null;

        bankTotal = bankTotalInStart;

        GameLoader.SoftResetGame();

        if (gameLoader != null)
            gameLoader.LoadJSON();
        else if (GameLoader.Instance != null)
            GameLoader.Instance.LoadJSON();

        GameLoader.remainingScenarioNames.Clear();

        foreach (string scenario in GameLoader.allScenarios.Keys)
            GameLoader.remainingScenarioNames.Add(scenario);

        GameLoader.LoadNextScenario();

        if (DynamicLevelUI.completedLevels != null)
            DynamicLevelUI.completedLevels.Clear();

        if (DynamicLevelUI.failedLevels != null)
            DynamicLevelUI.failedLevels.Clear();

        if (DynamicLevelUI.timeoutLevels != null)
            DynamicLevelUI.timeoutLevels.Clear();

        if (borrowPanel != null) borrowPanel.SetActive(false);
        if (borrowOptionsPanel != null) borrowOptionsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (roundOverUI != null) roundOverUI.SetActive(false);

        if (homeUI != null) homeUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);

        if (profitGraph != null)
        {
            profitGraph.ResetGraph();
            profitGraph.AddProfitPoint(0);
        }

        UpdateBalanceUI();

        DynamicLevelUI ui = homeUI != null
            ? homeUI.GetComponentInChildren<DynamicLevelUI>()
            : null;

        if (ui != null)
            ui.RefreshButtons();
    }

    private void SetCanvasState(CanvasGroup canvas, float alpha, bool interactable)
    {
        if (canvas == null) return;

        canvas.alpha = alpha;
        canvas.interactable = interactable;
        canvas.blocksRaycasts = interactable;
    }

    private IEnumerator FadeCanvas(
        CanvasGroup canvas,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (canvas == null) yield break;

        float elapsed = 0f;
        canvas.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvas.alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                duration > 0f ? elapsed / duration : 1f
            );

            yield return null;
        }

        SetCanvasState(canvas, endAlpha, endAlpha >= 1f);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}