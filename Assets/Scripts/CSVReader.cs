using System.Collections.Generic;
using System.Text;
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

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            List<string> data = ParseCSVLine(lines[i]);

            // Need exactly 7 columns
            if (data.Count < 7)
            {
                Debug.LogWarning("Invalid CSV line " + (i + 1) + " - skipped.");
                continue;
            }

            // Remove accidental spaces
            for (int j = 0; j < data.Count; j++)
            {
                data[j] = data[j].Trim();
            }

            // Skip incomplete questions
            if (string.IsNullOrWhiteSpace(data[1]) ||
                string.IsNullOrWhiteSpace(data[2]) ||
                string.IsNullOrWhiteSpace(data[3]) ||
                string.IsNullOrWhiteSpace(data[4]) ||
                string.IsNullOrWhiteSpace(data[5]) ||
                string.IsNullOrWhiteSpace(data[6]))
            {
                Debug.LogWarning("Incomplete question on CSV line " + (i + 1) + " - skipped.");
                continue;
            }

            // Check ID
            if (!int.TryParse(data[0], out int questionID))
            {
                Debug.LogWarning("Invalid question ID on CSV line " + (i + 1) + " - skipped.");
                continue;
            }

            // Check correct answer
            string correctAnswer = data[6].ToUpper();

            if (correctAnswer != "A" &&
                correctAnswer != "B" &&
                correctAnswer != "C" &&
                correctAnswer != "D")
            {
                Debug.LogWarning("Invalid correct answer on CSV line " + (i + 1) + " - skipped.");
                continue;
            }

            Question question = new Question();

            question.id = questionID;
            question.question = data[1];
            question.answerA = data[2];
            question.answerB = data[3];
            question.answerC = data[4];
            question.answerD = data[5];
            question.correct = correctAnswer;

            questions.Add(question);
        }

        Debug.Log("Loaded " + questions.Count + " valid questions.");
    }

    private List<string> ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        StringBuilder current = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];

            if (character == '"')
            {
                // Two quotation marks inside a quoted field = one quotation mark
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        result.Add(current.ToString());

        return result;
    }
}