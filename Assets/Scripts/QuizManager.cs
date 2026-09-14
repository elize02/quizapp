using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [SerializeField] private CSVReader csvReader;

    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text questionCounter;

    [SerializeField] private Transform answerContent;
    [SerializeField] private GameObject answerButtonPrefab;

    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text pointsText;

    [SerializeField] private GameObject pointsPopupPrefab;
    [SerializeField] private Transform popupParent;

    [SerializeField] private RectTransform progressBarFill;

    [SerializeField] private Image star1;
    [SerializeField] private Image star2;
    [SerializeField] private Image star3;

    [SerializeField] private Sprite emptyStar;
    [SerializeField] private Sprite fullStar;
    [SerializeField] private Animator star1Animator;
    [SerializeField] private Animator star2Animator;
    [SerializeField] private Animator star3Animator;

    private int currentQuestion = 0;
    private int points = 0;
    private float timer = 0f;

    private void Start()
    {
        pointsText.text = "00";
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(NextQuestion);

        ShowQuestion();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void ShowQuestion()
    {
        Question question = csvReader.questions[currentQuestion];

        questionText.text = question.question;

        questionCounter.text =
            "Question " + (currentQuestion + 1) + " / " + csvReader.questions.Count;

        // Remove previous answer buttons
        foreach (Transform child in answerContent)
        {
            Destroy(child.gameObject);
        }

        // Hide Next button
        nextButton.gameObject.SetActive(false);

        // Create answer buttons
        CreateAnswerButton(question.answerA, "A");
        CreateAnswerButton(question.answerB, "B");
        CreateAnswerButton(question.answerC, "C");
        CreateAnswerButton(question.answerD, "D");
    }

    private void CreateAnswerButton(string answer, string answerLetter)
    {
        GameObject newButton =
            Instantiate(answerButtonPrefab, answerContent);

        TMP_Text buttonText =
            newButton.GetComponentInChildren<TMP_Text>();

        buttonText.text = answer;

        Button button = newButton.GetComponent<Button>();

        button.onClick.AddListener(
            () => CheckAnswer(answerLetter, button)
        );
    }

    private void CheckAnswer(string selectedAnswer, Button selectedButton)
    {
        Question question = csvReader.questions[currentQuestion];

        // Prevent clicking multiple answers
        Button[] allButtons = answerContent.GetComponentsInChildren<Button>();

        foreach (Button button in allButtons)
        {
            button.interactable = false;
        }

        // Check answer
        if (selectedAnswer == question.correct)
        {
            points += 10;

            UpdateStars();

            pointsText.text = points.ToString();

            SetButtonColor(selectedButton, Color.green);

            ShowPointsPopup();

            Debug.Log("Correct! +" + 10 + " points");
        }
        else
        {
            // Selected answer is wrong → RED
            SetButtonColor(selectedButton, Color.red);

            // Find and highlight correct answer
            foreach (Button button in allButtons)
            {
                TMP_Text text =
                    button.GetComponentInChildren<TMP_Text>();

                string buttonText = text.text;

                if (buttonText == GetCorrectAnswerText(question))
                {
                    SetButtonColor(button, Color.green);
                }
            }

            Debug.Log("Wrong!");
        }

        UpdateProgressBar();

        // Show Next button
        nextButton.gameObject.SetActive(true);
    }

    private void ShowPointsPopup()
    {
        GameObject popup = Instantiate(pointsPopupPrefab, popupParent);
        popup.transform.SetAsLastSibling();
    }

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
        Image buttonImage = button.GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }

    private void NextQuestion()
    {
        currentQuestion++;

        if (currentQuestion < csvReader.questions.Count)
        {
            ShowQuestion();
        }
        else
        {
            FinishQuiz();
        }
    }

    private void UpdateProgressBar()
    {
        float progress =
            (float)(currentQuestion + 1) / csvReader.questions.Count;

        progressBarFill.anchorMax =
            new Vector2(progress, progressBarFill.anchorMax.y);
    }

    private void UpdateStars()
    {
        int correctAnswers = points / 10;

        if (correctAnswers >= 4 && star1.sprite != fullStar)
        {
            star1.sprite = fullStar;
            star1Animator.SetTrigger("Pop");
        }

        if (correctAnswers >= 7 && star2.sprite != fullStar)
        {
            star2.sprite = fullStar;
            star2Animator.SetTrigger("Pop");
        }

        if (correctAnswers >= 10 && star3.sprite != fullStar)
        {
            star3.sprite = fullStar;
            star3Animator.SetTrigger("Pop");
        }
    }

    private void FinishQuiz()
    {
        QuizData.points = points;
        QuizData.totalQuestions = csvReader.questions.Count;
        QuizData.completionTime = timer;

        Debug.Log("Quiz finished!");
        Debug.Log("Score: " + points);
        Debug.Log("Time: " + timer + " seconds");

        SceneManager.LoadScene(1);
    }
}