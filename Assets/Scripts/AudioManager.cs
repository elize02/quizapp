using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip resultsSound;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        UpdateVolume();

        AddClickSoundsToButtons();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateVolume();
        AddClickSoundsToButtons();
    }


    public void UpdateVolume()
    {
        audioSource.volume = SettingsManager.SFXVolume;
    }


    private void AddClickSoundsToButtons()
    {
        Button[] buttons = FindObjectsByType<Button>();

        foreach (Button button in buttons)
        {
            button.onClick.AddListener(PlayButtonClick);
        }
    }


    public void PlayButtonClick()
    {
        audioSource.PlayOneShot(buttonClickSound);
    }


    public void PlayCorrectSound()
    {
        audioSource.PlayOneShot(correctSound);
    }


    public void PlayWrongSound()
    {
        audioSource.PlayOneShot(wrongSound);
    }


    public void PlayResultsSound()
    {
        audioSource.PlayOneShot(resultsSound);
    }
}
