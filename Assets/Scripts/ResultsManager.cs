using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultsManager : MonoBehaviour
{
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private UnityEngine.UI.Button retryButton;

    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;

    [SerializeField] private Sprite emptyStar;
    [SerializeField] private Sprite fullStar;

    [SerializeField] private Animator star1Animator;
    [SerializeField] private Animator star2Animator;
    [SerializeField] private Animator star3Animator;

    [SerializeField] private TMP_Text gamificationUsedText;

    private void Start()
    {
        int points = QuizData.points;
        int total = QuizData.totalQuestions;

        pointsText.text = "  " + points.ToString();

        float accuracy = ((float)(points / 10) / total) * 100f;
        accuracyText.text = "  " + accuracy.ToString("0") + "%";

        int minutes = Mathf.FloorToInt(QuizData.completionTime / 60f);
        int seconds = Mathf.FloorToInt(QuizData.completionTime % 60f);

        timeText.text =
            "  " + minutes.ToString("00") + ":" + seconds.ToString("00");

        ShowStars();
        ShowGamificationUsed();

        AudioManager.Instance.PlayResultsSound();
    }

    private void ShowStars()
    {
        int correctAnswers = QuizData.points / 10;

        if (correctAnswers >= 4)
        {
            star1.sprite = fullStar;
            star1Animator.SetTrigger("Pop");
        }

        if (correctAnswers >= 7)
        {
            star2.sprite = fullStar;
            star2Animator.SetTrigger("Pop");
        }

        if (correctAnswers >= 10)
        {
            star3.sprite = fullStar;
            star3Animator.SetTrigger("Pop");
        }
    }

    private void ShowGamificationUsed()
    {
        string text = "  ";

        if (SettingsManager.PointsEnabled)
            text += "Points, ";

        if (SettingsManager.ProgressBarEnabled)
            text += "Progress Bar, ";

        if (SettingsManager.StarsEnabled)
            text += "Stars";

        text = text.TrimEnd(',', ' ');

        if (string.IsNullOrEmpty(text))
            text = "  -";

        gamificationUsedText.text = text;
    }
}