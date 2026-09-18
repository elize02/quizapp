using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class QuizDataUploader : MonoBehaviour
{
    public static QuizDataUploader Instance;

    private const int REQUEST_TIMEOUT = 10;
    private const int MAX_RETRIES = 1;

    private class AttemptData
    {
        public string url;
        public string participantID;
        public int attempts;

        public int row = 0;

        public int score;
        public int totalQuestions;
        public int correctAnswers;
        public float accuracy;
        public float completionTime;
        public string gamificationUsed;

        public bool startFinished = false;
        public bool completeQueued = false;
    }

    private Queue<AttemptData> attemptQueue =
        new Queue<AttemptData>();

    private bool isProcessing = false;

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

    public void StartAttempt(
        string url,
        string participantID,
        int attempts)
    {
        AttemptData data = new AttemptData();

        data.url = url;
        data.participantID = participantID;
        data.attempts = attempts;

        attemptQueue.Enqueue(data);

        Debug.Log(
            "Added attempt " + attempts +
            " to upload queue. Queue size: " +
            attemptQueue.Count
        );

        ProcessQueue();
    }

    public void CompleteAttempt(
        int attempts,
        int score,
        int totalQuestions,
        int correctAnswers,
        float accuracy,
        float completionTime,
        string gamificationUsed)
    {
        AttemptData target = null;

        // Find the exact attempt that was completed.
        foreach (AttemptData data in attemptQueue)
        {
            if (data.attempts == attempts &&
                !data.completeQueued)
            {
                target = data;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogError(
                "Could not find attempt " +
                attempts +
                " in upload queue."
            );

            return;
        }

        target.score = score;
        target.totalQuestions = totalQuestions;
        target.correctAnswers = correctAnswers;
        target.accuracy = accuracy;
        target.completionTime = completionTime;
        target.gamificationUsed = gamificationUsed;
        target.completeQueued = true;

        Debug.Log(
            "Completion added for attempt " +
            attempts
        );

        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (isProcessing)
            return;

        if (attemptQueue.Count == 0)
            return;

        isProcessing = true;

        StartCoroutine(ProcessCurrentAttempt());
    }

    private IEnumerator ProcessCurrentAttempt()
    {
        AttemptData data = attemptQueue.Peek();

        // -------------------------
        // START
        // -------------------------

        if (!data.startFinished)
        {
            yield return StartCoroutine(
                SendStart(data)
            );

            // Start failed.
            if (!data.startFinished)
            {
                isProcessing = false;
                yield break;
            }
        }

        // -------------------------
        // COMPLETE
        // -------------------------

        if (data.completeQueued)
        {
            bool completedSuccessfully = false;

            yield return StartCoroutine(
                SendComplete(
                    data,
                    success =>
                    {
                        completedSuccessfully = success;
                    }
                )
            );

            if (completedSuccessfully)
            {
                Debug.Log(
                    "Attempt " +
                    data.attempts +
                    " finished successfully."
                );
            }
            else
            {
                Debug.LogError(
                    "Attempt " +
                    data.attempts +
                    " failed after retries. Removing from queue so later attempts are not blocked."
                );
            }

            attemptQueue.Dequeue();
        }

        isProcessing = false;

        // Process next attempt.
        if (attemptQueue.Count > 0)
        {
            StartCoroutine(StartNextProcess());
        }
    }

    private IEnumerator StartNextProcess()
    {
        yield return null;

        ProcessQueue();
    }

    private IEnumerator SendStart(
    AttemptData data)
    {
        string url =
            data.url
            + "?action=start"
            + "&participantID="
            + UnityWebRequest.EscapeURL(
                data.participantID
            )
            + "&attempts="
            + data.attempts;

        Debug.Log(
            "[UPLOAD] START request for attempt "
            + data.attempts
        );

        for (int retry = 0; retry <= MAX_RETRIES; retry++)
        {
            if (retry > 0)
            {
                Debug.Log(
                    "[UPLOAD] Retrying START for attempt "
                    + data.attempts
                    + "..."
                );
            }

            UnityWebRequest request =
                UnityWebRequest.Get(url);

            request.timeout = REQUEST_TIMEOUT;

            yield return request.SendWebRequest();

            Debug.Log(
                "[UPLOAD] START result: "
                + request.result
                + " | HTTP: "
                + request.responseCode
                + " | Error: "
                + request.error
            );

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                string response =
                    request.downloadHandler.text;

                Debug.Log(
                    "[UPLOAD] START response: "
                    + response
                );

                StartAttemptResponse result =
                    JsonUtility.FromJson<StartAttemptResponse>(
                        response
                    );

                if (result.success)
                {
                    data.row = result.row;
                    data.startFinished = true;

                    Debug.Log(
                        "[UPLOAD] Attempt "
                        + data.attempts
                        + " assigned row "
                        + data.row
                    );

                    request.Dispose();
                    yield break;
                }

                Debug.LogError(
                    "[UPLOAD] Google rejected START: "
                    + response
                );
            }
            else
            {
                Debug.LogError(
                    "[UPLOAD] START failed for attempt "
                    + data.attempts
                    + ": "
                    + request.error
                );
            }

            request.Dispose();
        }

        Debug.LogError(
            "[UPLOAD] START permanently failed for attempt "
            + data.attempts
        );
    }

    private IEnumerator SendComplete(
    AttemptData data,
    System.Action<bool> callback)
    {
        string url =
            data.url
            + "?action=complete"
            + "&row=" + data.row
            + "&score=" + data.score
            + "&totalQuestions=" + data.totalQuestions
            + "&correctAnswers=" + data.correctAnswers
            + "&accuracy=" + data.accuracy
            + "&completionTime=" + data.completionTime
            + "&attempts=" + data.attempts
            + "&gamificationUsed="
            + UnityWebRequest.EscapeURL(
                data.gamificationUsed
            );

        Debug.Log(
            "[UPLOAD] COMPLETE request for attempt "
            + data.attempts
            + " on row "
            + data.row
        );

        for (int retry = 0; retry <= MAX_RETRIES; retry++)
        {
            if (retry > 0)
            {
                Debug.Log(
                    "[UPLOAD] Retrying COMPLETE for attempt "
                    + data.attempts
                    + "..."
                );
            }

            UnityWebRequest request =
                UnityWebRequest.Get(url);

            request.timeout = REQUEST_TIMEOUT;

            yield return request.SendWebRequest();

            Debug.Log(
                "[UPLOAD] COMPLETE result: "
                + request.result
                + " | HTTP: "
                + request.responseCode
                + " | Error: "
                + request.error
            );

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                string response =
                    request.downloadHandler.text;

                Debug.Log(
                    "[UPLOAD] COMPLETE response: "
                    + response
                );

                bool googleSuccess = false;

                try
                {
                    StartAttemptResponse result =
                        JsonUtility.FromJson<StartAttemptResponse>(
                            response
                        );

                    googleSuccess = result.success;
                }
                catch
                {
                    Debug.LogError(
                        "[UPLOAD] Could not read COMPLETE response."
                    );
                }

                request.Dispose();

                if (googleSuccess)
                {
                    Debug.Log(
                        "[UPLOAD] COMPLETE successful for attempt "
                        + data.attempts
                        + " on row "
                        + data.row
                    );

                    callback(true);
                    yield break;
                }

                Debug.LogError(
                    "[UPLOAD] Google returned success=false."
                );
            }

            request.Dispose();
        }

        Debug.LogError(
            "[UPLOAD] COMPLETE permanently failed for attempt "
            + data.attempts
            + ". Moving to next queue item."
        );

        callback(false);
    }

    [System.Serializable]
    private class StartAttemptResponse
    {
        public bool success;
        public int row;
    }
}