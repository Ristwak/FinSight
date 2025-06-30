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

    private List<CrisisQuestion> allQuestions = new List<CrisisQuestion>();
    private Action onCompleteCallback;

    private bool isCrisisActive = false;
    private int totalCrisisLoss = 0;
    private float lossTimer = 0f;
    private int questionsAnswered = 0;

    void Start()
    {
        loginPanel.SetActive(true);
        homeUI.SetActive(false);
        crisisPanel.SetActive(false);
    }

    void Update()
    {
        if (!isCrisisActive) return;

        lossTimer += Time.deltaTime;
        if (lossTimer >= lossIntervalSeconds)
        {
            lossTimer -= lossIntervalSeconds;

            GameLoader.playerBalance -= crisisLoss;
            totalCrisisLoss += crisisLoss;

            if (GameLoader.playerBalance < 0)
            {
                if (levelLoader.bankTotal > 0)
                {
                    levelLoader.bankTotal -= crisisLoss;
                    GameLoader.playerBalance = 0;
                }
                else
                {
                    // Game Over condition triggered
                    GameLoader.playerBalance = 0;

                    isCrisisActive = false;
                    crisisPanel.SetActive(false);

                    if (levelLoader != null && levelLoader.gameOverPanel != null)
                    {
                        homeUI.SetActive(false);
                        levelLoader.homeUI.SetActive(false);
                        levelLoader.gameUI.SetActive(false);
                        levelLoader.gameOverPanel.SetActive(true);
                        Debug.Log("💀 Game Over: Crisis caused total depletion of funds.");
                        Time.timeScale = 0f;
                    }

                    return;
                }
            }

            // ✅ Update visible UI balance and bank display
            if (levelLoader.balanceText != null)
                levelLoader.balanceText.text = GameLoader.playerBalance.ToString();

            if (levelLoader.BankText != null)
                levelLoader.BankText.text = levelLoader.bankTotal.ToString();

            Debug.Log($"Crisis loss so far: {totalCrisisLoss}");
        }

        balanceText.text = $"-{totalCrisisLoss}";
    }

    public void StartCrisis(Action onComplete)
    {
        onCompleteCallback = onComplete;
        LoadQuestionsFromJSON();

        questionsAnswered = 0;
        totalCrisisLoss = 0;
        lossTimer = 0f;
        isCrisisActive = true;

        foreach (var panel in panelsToDisable)
            if (panel != null) panel.SetActive(false);

        crisisPanel.SetActive(true);

        ShowNextQuestion();
    }

    void LoadQuestionsFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/CrisisQuestions");
        if (jsonFile == null)
        {
            Debug.LogError("CrisisManager: JSON file not found.");
            return;
        }

        CrisisQuestionData data = JsonUtility.FromJson<CrisisQuestionData>(jsonFile.text);
        allQuestions = data.questions;
    }

    void ShowNextQuestion()
    {
        if (questionsAnswered >= totalCrisisQuestions)
        {
            EndCrisis();
            return;
        }

        var q = allQuestions[UnityEngine.Random.Range(0, allQuestions.Count)];
        questionText.text = q.question;

        trueButton.onClick.RemoveAllListeners();
        falseButton.onClick.RemoveAllListeners();

        trueButton.onClick.AddListener(() => HandleAnswer(true, q.answer));
        falseButton.onClick.AddListener(() => HandleAnswer(false, q.answer));
    }

    void HandleAnswer(bool selected, bool correct)
    {
        questionsAnswered++;
        ShowNextQuestion();
    }

    void EndCrisis()
    {
        isCrisisActive = false;
        crisisPanel.SetActive(false);

        foreach (var panel in panelsToDisable)
            if (panel != null) panel.SetActive(true);

        if (homeUI != null) homeUI.SetActive(true);

        if (graphUI != null)
        {
            var graph = graphUI.GetComponent<AnimatedLineGraph>();
            if (graph != null)
            {
                graph.AddProfitPoint(GameLoader.playerBalance);  // Add latest balance after crisis
            }
        }

        Debug.Log($"Crisis completed. Total profit lost: {totalCrisisLoss}");

        // ✅ If player is in debt, trigger borrow UI
        if (GameLoader.playerBalance < 0 && levelLoader != null)
        {
            levelLoader.TriggerBorrowPanel();  // <-- Must be public if not already
            return; // Prevent auto-continuation
        }

        onCompleteCallback?.Invoke(); // Only proceed if not in debt
    }
}
