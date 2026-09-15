using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public enum SettingType
    {
        Points,
        ProgressBar,
        Stars
    }

    [Header("SFX Volume")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeText;

    [Header("Points")]
    [SerializeField] private Button pointsOnButton;
    [SerializeField] private Button pointsOffButton;

    [Header("Progress Bar")]
    [SerializeField] private Button progressOnButton;
    [SerializeField] private Button progressOffButton;

    [Header("Stars")]
    [SerializeField] private Button starsOnButton;
    [SerializeField] private Button starsOffButton;

    [Header("Button Colors")]
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color normalColor;

    public static bool PointsEnabled
    {
        get => PlayerPrefs.GetInt("PointsEnabled", 1) == 1;
    }

    public static bool ProgressBarEnabled
    {
        get => PlayerPrefs.GetInt("ProgressBarEnabled", 1) == 1;
    }

    public static bool StarsEnabled
    {
        get => PlayerPrefs.GetInt("StarsEnabled", 1) == 1;
    }

    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void Start()
    {
        // Load saved volume
        volumeSlider.value = SFXVolume;
        UpdateVolumeText();

        volumeSlider.onValueChanged.AddListener(SetVolume);

        // Connect buttons
        pointsOnButton.onClick.AddListener(() => SetSetting("PointsEnabled", true));
        pointsOffButton.onClick.AddListener(() => SetSetting("PointsEnabled", false));

        progressOnButton.onClick.AddListener(() => SetSetting("ProgressBarEnabled", true));
        progressOffButton.onClick.AddListener(() => SetSetting("ProgressBarEnabled", false));

        starsOnButton.onClick.AddListener(() => SetSetting("StarsEnabled", true));
        starsOffButton.onClick.AddListener(() => SetSetting("StarsEnabled", false));

        UpdateAllButtons();
    }

    private void SetVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();

        UpdateVolumeText();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateVolume();
        }
    }

    private void UpdateVolumeText()
    {
        int percentage = Mathf.RoundToInt(volumeSlider.value * 100f);
        volumeText.text = percentage + "%";
    }

    private void SetSetting(string settingName, bool value)
    {
        PlayerPrefs.SetInt(settingName, value ? 1 : 0);
        PlayerPrefs.Save();

        UpdateAllButtons();
    }

    private void UpdateAllButtons()
    {
        UpdateButtonColors(
            pointsOnButton,
            pointsOffButton,
            PointsEnabled
        );

        UpdateButtonColors(
            progressOnButton,
            progressOffButton,
            ProgressBarEnabled
        );

        UpdateButtonColors(
            starsOnButton,
            starsOffButton,
            StarsEnabled
        );
    }

    private void UpdateButtonColors(
        Button onButton,
        Button offButton,
        bool isOn)
    {
        onButton.GetComponent<Image>().color =
            isOn ? selectedColor : normalColor;

        offButton.GetComponent<Image>().color =
            isOn ? normalColor : selectedColor;
    }
}