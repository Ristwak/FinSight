using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;           // For button clicks, answers, etc.
    public AudioSource musicSource;         // For background music
    public AudioSource graphSFXSource;      // For graph draw sound (lower volume)
    private AudioSource crisisLoopSource;   // Separate crisis loop audio

    [Header("Sound Clips")]
    public AudioClip correctAnswerClip;
    public AudioClip wrongAnswerClip;
    public AudioClip buttonClickClip;
    public AudioClip crisisLoopClip;
    public AudioClip graphDrawClip;

    [Header("Music")]
    public AudioClip defaultBGM;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes

            // Setup music
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 0.2f;
            }

            // Setup SFX
            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
                sfxSource.volume = 1f;
            }

            // Setup graph sound source
            if (graphSFXSource != null)
            {
                graphSFXSource.playOnAwake = false;
                graphSFXSource.volume = 0.2f; // Lower volume for graph
            }

            PlayMusic(defaultBGM);
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    // 🔊 Play background music
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null && musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource?.Stop();
    }

    // 🔊 General SFX
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // 🔊 Predefined SFX
    public void PlayButtonClick() => PlaySFX(buttonClickClip);
    public void PlayCorrectAnswer() => PlaySFX(correctAnswerClip);
    public void PlayWrongAnswer() => PlaySFX(wrongAnswerClip);

    // 🔁 Crisis Loop
    public void StartCrisisLoop()
    {
        if (crisisLoopClip != null)
        {
            crisisLoopSource = gameObject.AddComponent<AudioSource>();
            crisisLoopSource.clip = crisisLoopClip;
            crisisLoopSource.loop = true;
            crisisLoopSource.volume = 0.6f;
            crisisLoopSource.Play();
        }
    }

    public void StopCrisisLoop()
    {
        if (crisisLoopSource != null)
        {
            crisisLoopSource.Stop();
            Destroy(crisisLoopSource);
        }
    }

    // 🎧 Graph draw sound
    public void PlayGraphDrawSound()
    {
        if (graphSFXSource != null && graphDrawClip != null)
        {
            graphSFXSource.PlayOneShot(graphDrawClip);
        }
    }
}
