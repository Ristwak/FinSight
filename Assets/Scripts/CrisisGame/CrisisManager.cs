using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CrisisQuestion
{
    public string question;
    public bool answer;
}

[System.Serializable]
public class CrisisQuestionData
{
    public List<CrisisQuestion> questions;
}

public class CrisisManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject crisisPanel;
    public GameObject homeUI;
    public GameObject graphUI;
    public LevelLoader levelLoader;

    [Header("Texts")]
    public Text questionText;
    public Text balanceText;

    [Header("Buttons")]
    public Button trueButton;
    public Button falseButton;

    [Header("Other Panels to Disable")]
    public List<GameObject> panelsToDisable;

    [Header("Crisis Settings")]
    public int totalCrisisQuestions = 5;
    public int crisisLoss = 1;
    public float lossIntervalSeconds = 1f;

    private List<CrisisQuestion> allQuestions = new();
    private Action onCompleteCallback;

    private bool isCrisisActive;
    private int totalCrisisLoss;
    private float lossTimer;
    private int questionsAnswered;

    private void Start()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (homeUI != null) homeUI.SetActive(false);
        if (crisisPanel != null) crisisPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isCrisisActive)
            return;

        if (levelLoader == null)
        {
            Debug.LogError("CrisisManager: LevelLoader reference is missing.");
            FinishCrisis(true);
            return;
        }

        lossTimer += Time.deltaTime;

        float interval = Mathf.Max(0.05f, lossIntervalSeconds);

        while (lossTimer >= interval && isCrisisActive)
        {
            lossTimer -= interval;
            ApplyCrisisLoss();
        }

        if (balanceText != null)
            balanceText.text = totalCrisisLoss.ToString();
    }

    private void ApplyCrisisLoss()
    {
        int amount = Mathf.Max(0, crisisLoss);

        if (amount == 0)
            return;

        GameLoader.playerBalance -= amount;
        totalCrisisLoss += amount;

        if (GameLoader.playerBalance < 0)
        {
            int debt = Mathf.Abs(GameLoader.playerBalance);
            int bankPayment = Mathf.Min(debt, levelLoader.bankTotal);

            levelLoader.bankTotal -= bankPayment;
            GameLoader.playerBalance += bankPayment;
        }

        if (levelLoader.balanceText != null)
            levelLoader.balanceText.text = GameLoader.playerBalance.ToString();

        if (levelLoader.BankText != null)
            levelLoader.BankText.text = levelLoader.bankTotal.ToString();

        if (GameLoader.playerBalance < 0 && levelLoader.bankTotal <= 0)
        {
            isCrisisActive = false;

            if (crisisPanel != null)
                crisisPanel.SetActive(false);

            if (levelLoader.gameOverPanel != null)
                levelLoader.gameOverPanel.SetActive(true);

            if (levelLoader.homeUI != null)
                levelLoader.homeUI.SetActive(false);

            if (levelLoader.gameUI != null)
                levelLoader.gameUI.SetActive(false);

            if (homeUI != null)
                homeUI.SetActive(false);

            levelLoader.isGameOver = true;
            Time.timeScale = 0f;

            Debug.Log("Game Over: Crisis depleted the player's available funds.");
        }
    }

    public void StartCrisis(Action onComplete)
    {
        if (isCrisisActive)
        {
            Debug.LogWarning("A crisis is already active.");
            return;
        }

        if (!LoadQuestionsFromJSON())
        {
            Debug.LogError("Crisis could not start because question data is unavailable.");
            onComplete?.Invoke();
            return;
        }

        if (crisisPanel == null || questionText == null ||
            trueButton == null || falseButton == null)
        {
            Debug.LogError("CrisisManager UI references are incomplete.");
            onComplete?.Invoke();
            return;
        }

        onCompleteCallback = onComplete;

        questionsAnswered = 0;
        totalCrisisLoss = 0;
        lossTimer = 0f;
        isCrisisActive = true;

        foreach (GameObject panel in panelsToDisable)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        crisisPanel.SetActive(true);
        ShowNextQuestion();
    }

    private bool LoadQuestionsFromJSON()
    {
        TextAsset jsonFile =
            Resources.Load<TextAsset>("Data/CrisisQuestions");

        if (jsonFile == null)
        {
            Debug.LogError("CrisisQuestions JSON not found in Resources/Data.");
            return false;
        }

        try
        {
            CrisisQuestionData data =
                JsonUtility.FromJson<CrisisQuestionData>(jsonFile.text);

            if (data == null || data.questions == null ||
                data.questions.Count == 0)
            {
                Debug.LogError("CrisisQuestions JSON contains no questions.");
                return false;
            }

            allQuestions = data.questions.FindAll(
                q => q != null && !string.IsNullOrWhiteSpace(q.question)
            );

            return allQuestions.Count > 0;
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to parse crisis questions: " + exception.Message);
            return false;
        }
    }

    private void ShowNextQuestion()
    {
        if (!isCrisisActive)
            return;

        if (questionsAnswered >= Mathf.Max(1, totalCrisisQuestions))
        {
            EndCrisis();
            return;
        }

        if (allQuestions == null || allQuestions.Count == 0)
        {
            EndCrisis();
            return;
        }

        CrisisQuestion question =
            allQuestions[UnityEngine.Random.Range(0, allQuestions.Count)];

        questionText.text = question.question;

        trueButton.onClick.RemoveAllListeners();
        falseButton.onClick.RemoveAllListeners();

        trueButton.onClick.AddListener(
            () => HandleAnswer(true, question.answer)
        );

        falseButton.onClick.AddListener(
            () => HandleAnswer(false, question.answer)
        );
    }

    private void HandleAnswer(bool selected, bool correct)
    {
        if (!isCrisisActive)
            return;

        questionsAnswered++;

        trueButton.interactable = false;
        falseButton.interactable = false;

        ShowNextQuestion();

        if (isCrisisActive)
        {
            trueButton.interactable = true;
            falseButton.interactable = true;
        }
    }

    private void EndCrisis()
    {
        FinishCrisis(true);
    }

    private void FinishCrisis(bool invokeCallback)
    {
        isCrisisActive = false;

        if (crisisPanel != null)
            crisisPanel.SetActive(false);

        foreach (GameObject panel in panelsToDisable)
        {
            if (panel != null)
                panel.SetActive(true);
        }

        if (homeUI != null)
            homeUI.SetActive(true);

        if (graphUI != null)
        {
            AnimatedLineGraph graph =
                graphUI.GetComponent<AnimatedLineGraph>();

            if (graph != null)
                graph.AddProfitPoint(GameLoader.playerBalance);
        }

        Debug.Log($"Crisis completed. Total loss: {totalCrisisLoss}");

        Action callback = onCompleteCallback;
        onCompleteCallback = null;

        if (!invokeCallback)
            return;

        if (GameLoader.playerBalance < 0 && levelLoader != null)
        {
            levelLoader.onBorrowCompleted = callback;
            levelLoader.TriggerBorrowPanel();
            return;
        }

        callback?.Invoke();
    }
}