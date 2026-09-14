using System.Collections.Generic;
using UnityEngine;

public class CSVReader : MonoBehaviour
{
    [SerializeField] private TextAsset questionFile;

    public List<Question> questions = new List<Question>();

    private void Awake()
    {
        LoadQuestions();
    }

    private void LoadQuestions()
    {
        if (questionFile == null)
        {
            Debug.LogError("No question CSV file assigned!");
            return;
        }

        string[] lines = questionFile.text.Split('\n');

        // Start at 1 because row 0 contains the column names
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] data = lines[i].Trim().Split(',');

            if (data.Length < 7)
            {
                Debug.LogWarning("Invalid question on CSV line " + (i + 1));
                continue;
            }

            Question question = new Question();

            question.id = int.Parse(data[0]);
            question.question = data[1];
            question.answerA = data[2];
            question.answerB = data[3];
            question.answerC = data[4];
            question.answerD = data[5];
            question.correct = data[6].Trim();

            questions.Add(question);
        }

        Debug.Log("Loaded " + questions.Count + " questions.");
    }
}