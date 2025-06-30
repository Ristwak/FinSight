using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class WarManager : MonoBehaviour
{
    [Header("Reference")]
    public LevelLoader levelLoader;
    public GameLoader gameLoader;

    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject warPanel;
    public CanvasGroup homeUICanvasGroup;
    public Text streakConditionText;
    public Text crisisText;

    public Button okButton;

    [Header("War Settings")]
    public int requiredStreakMin = 3;
    public int requiredStreakMax = 6;

    private int requiredStreak;
    private Action onWarComplete;
    public CrisisEventData crisisEventData;

    void Start()
    {
        loginPanel.SetActive(true);
        warPanel.SetActive(false);
        okButton.onClick.AddListener(HandleOkButton);
        LoadPortfolioData();
    }

    void LoadPortfolioData()
    {
        // Do NOT include folder "Resources" or file extension ".json"
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/CrisisEvents");

        if (jsonAsset != null)
        {
            crisisEventData = JsonUtility.FromJson<CrisisEventData>(jsonAsset.text);
            Debug.Log("Crisis data loaded successfully.");
        }
        else
        {
            Debug.LogError("Failed to load CrisisEvents from Resources/Data folder.");
        }
    }

    public void StartWar(Action onComplete)
    {
        onWarComplete = onComplete;
        requiredStreak = UnityEngine.Random.Range(requiredStreakMin, requiredStreakMax); // Adjust as needed
        streakConditionText.text = "Required Streak: " + requiredStreak;

        foreach (var panel in crisisEventData.crisisEvents)
        {
            crisisText.text = panel.text;
        }

        warPanel.SetActive(true);
        StartCoroutine(FadeCanvas(homeUICanvasGroup, 1f, 0.4f, 0.3f));
    }

    void HandleOkButton()
    {
        warPanel.SetActive(false);
        StartCoroutine(FadeCanvas(homeUICanvasGroup, 0.4f, 1f, 0.3f));

        if (GameLoader.currentStreak < requiredStreak)
        {
            Debug.Log("War failed. Restarting game...");
            // gameLoader.ResetStatics();
            levelLoader.ResetGame();
        }
        else
        {
            Debug.Log("War survived. Continuing game.");
            onWarComplete?.Invoke();
        }
    }

    IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            time += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
        canvasGroup.interactable = endAlpha == 1f;
        canvasGroup.blocksRaycasts = endAlpha == 1f;
    }
}

[System.Serializable]
public class CrisisEvent
{
    public string text;
}

[System.Serializable]
public class CrisisEventData
{
    public List<CrisisEvent> crisisEvents;
}
