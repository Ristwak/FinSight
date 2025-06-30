using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Text errorMessageText;

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject homePanel;
    public GameObject aboutPanel;
    
    [Header("Reference")]
    public SoundManager soundManager;
    public AudioClip bgAudioClip;
    public AudioClip loginAudioClip;

    // [Header("Authentication")]
    // public string expectedPassword = "1234";

    private void Start()
    {
        loginButton.onClick.AddListener(HandleLogin);
        errorMessageText.text = "";

        // Ensure login panel is active and home is hidden at start
        loginPanel.SetActive(true);
        aboutPanel.SetActive(false);
        homePanel.SetActive(false);
        soundManager.PlayMusic(loginAudioClip);
    }

    void HandleLogin()
    {
        string username = usernameInput.text.Trim();
        string enteredPassword = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(enteredPassword))
        {
            errorMessageText.text = "Please enter both username and password.";
            return;
        }

        // if (enteredPassword != Null)
        // {
        //     Debug.Log("Login successful!");
        //     errorMessageText.text = "";

        //     // Switch panels
        //     loginPanel.SetActive(false);
        //     aboutPanel.SetActive(true);
        //     homePanel.SetActive(false);
        // }
        // else
        // {
        //     errorMessageText.text = "Invalid username or password.";
        // }
        Debug.Log("Login successful!");
        errorMessageText.text = "";

        // Switch panels
        loginPanel.SetActive(false);
        aboutPanel.SetActive(true);
        homePanel.SetActive(false);
        soundManager.PlayButtonClick();
        soundManager.PlayMusic(bgAudioClip);
    }

    public void HandleAboutContinue()
    {
        soundManager.PlayButtonClick();
        aboutPanel.SetActive(false);
        homePanel.SetActive(true); // Show main UI
    }
}
