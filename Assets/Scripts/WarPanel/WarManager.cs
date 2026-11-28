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
    private bool isWarActive;
    private Coroutine fadeCoroutine;

    public CrisisEventData crisisEventData;

    private void Start()
    {
        if (loginPanel != null)
            loginPanel.SetActive(true);

        if (warPanel != null)
            warPanel.SetActive(false);

        if (okButton != null)
        {
            okButton.onClick.RemoveListener(HandleOkButton);
            okButton.onClick.AddListener(HandleOkButton);
        }

        LoadPortfolioData();
    }

    private void LoadPortfolioData()
    {
        TextAsset jsonAsset =
            Resources.Load<TextAsset>("Data/CrisisEvents");

        if (jsonAsset == null)
        {
            Debug.LogError("CrisisEvents JSON not found in Resources/Data.");
            return;
        }

        try
        {
            crisisEventData =
                JsonUtility.FromJson<CrisisEventData>(jsonAsset.text);

            if (crisisEventData == null ||
                crisisEventData.crisisEvents == null ||
                crisisEventData.crisisEvents.Count == 0)
            {
                Debug.LogError("CrisisEvents JSON has no events.");
                crisisEventData = null;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to parse CrisisEvents JSON: " + exception.Message);
            crisisEventData = null;
        }
    }

    public void StartWar(Action onComplete)
    {
        if (isWarActive)
        {
            Debug.LogWarning("War event is already active.");
            return;
        }

        if (warPanel == null || homeUICanvasGroup == null ||
            streakConditionText == null || crisisText == null)
        {
            Debug.LogError("WarManager UI references are incomplete.");
            onComplete?.Invoke();
            return;
        }

        if (crisisEventData == null ||
            crisisEventData.crisisEvents == null ||
            crisisEventData.crisisEvents.Count == 0)
        {
            LoadPortfolioData();

            if (crisisEventData == null)
            {
                Debug.LogWarning("War event data unavailable. Skipping war.");
                onComplete?.Invoke();
                return;
            }
        }

        onWarComplete = onComplete;
        isWarActive = true;

        int min = Mathf.Max(1, requiredStreakMin);
        int max = Mathf.Max(min, requiredStreakMax);

        // Random.Range for integers uses an exclusive upper bound.
        requiredStreak = UnityEngine.Random.Range(min, max + 1);

        streakConditionText.text = "Required Streak: " + requiredStreak;

        int randomEventIndex =
            UnityEngine.Random.Range(0, crisisEventData.crisisEvents.Count);

        crisisText.text =
            crisisEventData.crisisEvents[randomEventIndex].text;

        warPanel.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(
            FadeCanvas(homeUICanvasGroup, 1f, 0.4f, 0.3f)
        );
    }

    private void HandleOkButton()
    {
        if (!isWarActive)
            return;

        isWarActive = false;

        if (warPanel != null)
            warPanel.SetActive(false);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (homeUICanvasGroup != null)
        {
            fadeCoroutine = StartCoroutine(
                FadeCanvas(homeUICanvasGroup, 0.4f, 1f, 0.3f)
            );
        }

        Action callback = onWarComplete;
        onWarComplete = null;

        if (GameLoader.currentStreak < requiredStreak)
        {
            Debug.Log("War failed. Restarting game...");

            if (levelLoader != null)
            {
                levelLoader.ResetGame();
                callback?.Invoke();
            }
            else
            {
                Debug.LogError("LevelLoader reference is missing.");
            }
        }
        else
        {
            Debug.Log("War survived. Continuing game.");
            callback?.Invoke();
        }
    }

    private IEnumerator FadeCanvas(
        CanvasGroup canvasGroup,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = duration > 0f
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        canvasGroup.interactable = endAlpha >= 1f;
        canvasGroup.blocksRaycasts = endAlpha >= 1f;

        fadeCoroutine = null;
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