using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNav : MonoBehaviour
{
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void GoToAbout()
    {
        SceneManager.LoadScene(1);
    }

    public void GoToSettings()
    {
        SceneManager.LoadScene(2);
    }

    public void GoToQuiz()
    {
        SceneManager.LoadScene(3);
    }

    public void OpenFeedback()
    {
        Application.OpenURL(AppConfig.FeedbackFormUrl);
    }
}