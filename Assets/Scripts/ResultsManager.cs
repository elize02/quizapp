using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultsManager : MonoBehaviour
{
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private UnityEngine.UI.Button retryButton;

    private void Start()
    {
        int points = QuizData.points;
        int total = QuizData.totalQuestions;

        pointsText.text = "  " + (QuizData.points * 10).ToString();

        float accuracy = ((float)points / total) * 100f;
        accuracyText.text = "  " + accuracy.ToString("0") + "%";

        int minutes = Mathf.FloorToInt(QuizData.completionTime / 60f);
        int seconds = Mathf.FloorToInt(QuizData.completionTime % 60f);

        timeText.text = "  " + minutes.ToString("00") + ":" + seconds.ToString("00");

        retryButton.onClick.AddListener(RetryQuiz);
    }

    private void RetryQuiz()
    {
        SceneManager.LoadScene(0);
    }
}