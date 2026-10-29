using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Quiz")]
    [SerializeField] private CSVReader csvReader;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text questionCounter;
    [SerializeField] private Transform answerContent;
    [SerializeField] private GameObject answerButtonPrefab;
    [SerializeField] private Button nextButton;

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;

    [Header("Points")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private GameObject pointsPopupPrefab;
    [SerializeField] private Transform popupParent;
    [SerializeField] private GameObject pointsContainer;

    [Header("Progress Bar")]
    [SerializeField] private RectTransform progressBarFill;
    [SerializeField] private GameObject progressContainer;

    [Header("Stars")]
    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;
    [SerializeField] private Sprite fullStar;
    [SerializeField] private Animator star1Animator;
    [SerializeField] private Animator star2Animator;
    [SerializeField] private Animator star3Animator;
    [SerializeField] private GameObject starsContainer;

    private List<Question> shuffledQuestions;

    private int currentQuestion = 0;
    private int correctAnswers = 0;
    private int points = 0;
    private float timer = 0f;
    private float questionTimer = 0f;


    private class AnswerOption
    {
        public string text;
        public string letter;

        public AnswerOption(string text, string letter)
        {
            this.text = text;
            this.letter = letter;
        }
    }


    private void Start()
    {
        SetParticipantID();
        SetAttempt();

        QuizDataUploader.Instance.StartAttempt(
            AppConfig.GoogleAppsScriptUrl,
            QuizData.participantID,
            QuizData.attempts
            );

        ApplySettings();

        shuffledQuestions = new List<Question>(csvReader.questions);

        ShuffleQuestions();

        if (shuffledQuestions.Count > 10)
        {
            shuffledQuestions.RemoveRange(10, shuffledQuestions.Count - 10);
        }

        pointsText.text = "00";

        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(NextQuestion);

        ShowQuestion();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        questionTimer += Time.deltaTime;

        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }


    // -------------------------
    // SETTINGS
    // -------------------------

    private void ApplySettings()
    {
        pointsContainer.SetActive(SettingsManager.PointsEnabled);
        progressContainer.SetActive(SettingsManager.ProgressBarEnabled);
        starsContainer.SetActive(SettingsManager.StarsEnabled);
    }


    // -------------------------
    // QUESTIONS
    // -------------------------

    private void ShuffleQuestions()
    {
        for (int i = 0; i < shuffledQuestions.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledQuestions.Count);

            Question temp = shuffledQuestions[i];
            shuffledQuestions[i] = shuffledQuestions[randomIndex];
            shuffledQuestions[randomIndex] = temp;
        }
    }


    private void ShowQuestion()
    {
        questionTimer = 0f;

        Question question = shuffledQuestions[currentQuestion];

        questionText.text = question.question;

        questionCounter.text =
            "Question " + (currentQuestion + 1) +
            " / " + shuffledQuestions.Count;

        ClearAnswerButtons();

        nextButton.gameObject.SetActive(false);

        CreateShuffledAnswers(question);
    }


    private void ClearAnswerButtons()
    {
        foreach (Transform child in answerContent)
        {
            Destroy(child.gameObject);
        }
    }


    private void CreateShuffledAnswers(Question question)
    {
        List<AnswerOption> answers = new List<AnswerOption>
        {
            new AnswerOption(question.answerA, "A"),
            new AnswerOption(question.answerB, "B"),
            new AnswerOption(question.answerC, "C"),
            new AnswerOption(question.answerD, "D")
        };

        ShuffleAnswers(answers);

        foreach (AnswerOption answer in answers)
        {
            CreateAnswerButton(answer);
        }
    }


    private void ShuffleAnswers(List<AnswerOption> answers)
    {
        for (int i = 0; i < answers.Count; i++)
        {
            int randomIndex = Random.Range(i, answers.Count);

            AnswerOption temp = answers[i];
            answers[i] = answers[randomIndex];
            answers[randomIndex] = temp;
        }
    }


    private void CreateAnswerButton(AnswerOption answer)
    {
        GameObject newButton =
            Instantiate(answerButtonPrefab, answerContent);

        TMP_Text buttonText =
            newButton.GetComponentInChildren<TMP_Text>();

        buttonText.text = answer.text;

        Button button =
            newButton.GetComponent<Button>();

        button.onClick.AddListener(
            () => CheckAnswer(answer.letter, button)
        );
    }


    // -------------------------
    // ANSWER CHECKING
    // -------------------------

    private void CheckAnswer(string selectedAnswer, Button selectedButton)
    {
        Question question = shuffledQuestions[currentQuestion];

        Button[] allButtons =
            answerContent.GetComponentsInChildren<Button>();

        DisableAnswerButtons(allButtons);

        if (selectedAnswer == question.correct)
        {
            HandleCorrectAnswer(selectedButton);
        }
        else
        {
            HandleWrongAnswer(selectedButton, allButtons, question);
        }

        UpdateProgressBar();

        nextButton.gameObject.SetActive(true);
    }


    private void DisableAnswerButtons(Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            button.interactable = false;
        }
    }


    private void HandleCorrectAnswer(Button selectedButton)
    {
        correctAnswers++;
        int earnedPoints;

        if (questionTimer < 5f)
        {
            earnedPoints = 20;
        }
        else
        {
            earnedPoints = 10;
        }

        points += earnedPoints;

        SetButtonColor(selectedButton, Color.green);

        AudioManager.Instance.PlayCorrectSound();

        if (SettingsManager.PointsEnabled)
        {
            pointsText.text = points.ToString();
            ShowPointsPopup(earnedPoints);
        }

        if (SettingsManager.StarsEnabled)
        {
            UpdateStars();
        }

        Debug.Log("Correct! +" + earnedPoints + " points");
    }


    private void HandleWrongAnswer(
        Button selectedButton,
        Button[] allButtons,
        Question question)
    {
        SetButtonColor(selectedButton, Color.red);

        AudioManager.Instance.PlayWrongSound();

        string correctAnswer =
            GetCorrectAnswerText(question);

        foreach (Button button in allButtons)
        {
            TMP_Text text =
                button.GetComponentInChildren<TMP_Text>();

            if (text.text == correctAnswer)
            {
                SetButtonColor(button, Color.green);
                break;
            }
        }

        Debug.Log("Wrong!");
    }


    // -------------------------
    // GAMIFICATION
    // -------------------------

    private void ShowPointsPopup(int earnedPoints)
    {
        GameObject popup =
            Instantiate(pointsPopupPrefab, popupParent);

        TMP_Text popupText =
            popup.GetComponent<TMP_Text>();

        popupText.text = "+" + earnedPoints;

        popup.transform.SetAsLastSibling();
    }


    private void UpdateProgressBar()
    {
        if (!SettingsManager.ProgressBarEnabled)
            return;

        float progress =
            (float)(currentQuestion + 1) /
            shuffledQuestions.Count;

        progressBarFill.anchorMax =
            new Vector2(progress, progressBarFill.anchorMax.y);
    }


    private void UpdateStars()
    {
        if (correctAnswers >= 4 &&
            star1.sprite != fullStar)
        {
            star1.sprite = fullStar;
            star1Animator.SetTrigger("Pop");
        }

        if (correctAnswers >= 7 &&
            star2.sprite != fullStar)
        {
            star2.sprite = fullStar;
            star2Animator.SetTrigger("Pop");
        }

        if (correctAnswers >= 10 &&
            star3.sprite != fullStar)
        {
            star3.sprite = fullStar;
            star3Animator.SetTrigger("Pop");
        }
    }


    // -------------------------
    // HELPERS
    // -------------------------

    private string GetCorrectAnswerText(Question question)
    {
        switch (question.correct)
        {
            case "A":
                return question.answerA;

            case "B":
                return question.answerB;

            case "C":
                return question.answerC;

            case "D":
                return question.answerD;

            default:
                return "";
        }
    }


    private void SetButtonColor(Button button, Color color)
    {
        Image buttonImage =
            button.GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }

    private void SetParticipantID()
    {
        if (PlayerPrefs.HasKey("ParticipantID"))
        {
            QuizData.participantID = PlayerPrefs.GetString("ParticipantID");
        }
        else
        {
            QuizData.participantID =
                System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            PlayerPrefs.SetString("ParticipantID", QuizData.participantID);
            PlayerPrefs.Save();
        }

        Debug.Log("Participant ID: " + QuizData.participantID);
    }

    private void SetAttempt()
    {
        int attempts = PlayerPrefs.GetInt("Attempts", 0);

        attempts++;

        PlayerPrefs.SetInt("Attempts", attempts);
        PlayerPrefs.Save();

        QuizData.attempts = attempts;

        Debug.Log("Attempt: " + QuizData.attempts);
    }


    // -------------------------
    // NEXT / FINISH
    // -------------------------

    private void NextQuestion()
    {
        currentQuestion++;

        if (currentQuestion < shuffledQuestions.Count)
        {
            ShowQuestion();
        }
        else
        {
            FinishQuiz();
        }
    }

    private void FinishQuiz()
    {
        QuizData.points = points;
        QuizData.totalQuestions = shuffledQuestions.Count;
        QuizData.correctAnswers = correctAnswers;
        QuizData.completionTime = timer;

        CompleteAttempt();
    }

    private void CompleteAttempt()
    {
        float accuracy =
            ((float)correctAnswers / shuffledQuestions.Count) * 100f;

        string gamificationUsed = "";

        if (SettingsManager.PointsEnabled)
            gamificationUsed += "Points, ";

        if (SettingsManager.ProgressBarEnabled)
            gamificationUsed += "Progress Bar, ";

        if (SettingsManager.StarsEnabled)
            gamificationUsed += "Stars";

        gamificationUsed = gamificationUsed.TrimEnd(',', ' ');

        if (string.IsNullOrEmpty(gamificationUsed))
            gamificationUsed = "-";

        QuizDataUploader.Instance.CompleteAttempt(
            QuizData.attempts,
            points,
            shuffledQuestions.Count,
            correctAnswers,
            accuracy,
            timer,
            gamificationUsed
        );

        SceneManager.LoadScene(4);
    }
}