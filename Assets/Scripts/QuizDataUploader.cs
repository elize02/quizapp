using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class QuizDataUploader : MonoBehaviour
{
    public static QuizDataUploader Instance;

    private const int REQUEST_TIMEOUT = 10;
    private const int MAX_RETRIES = 1;

    // =========================================================
    // ATTEMPT DATA
    // =========================================================

    private class AttemptData
    {
        public string url;
        public string participantID;
        public int attempts;

        // Google Sheets row assigned by START
        public int row = 0;

        // Completion data
        public int score;
        public int totalQuestions;
        public int correctAnswers;
        public float accuracy;
        public float completionTime;
        public string gamificationUsed;

        // START state
        public bool startFinished = false;
        public bool startFailed = false;

        // COMPLETE state
        public bool completeQueued = false;
    }

    // =========================================================
    // REQUEST DATA
    // =========================================================

    private enum RequestType
    {
        Start,
        Complete
    }

    private class QueuedRequest
    {
        public RequestType type;
        public AttemptData attempt;
    }

    // =========================================================
    // QUEUE / ACTIVE ATTEMPTS
    // =========================================================

    // Requests are processed strictly in the order they arrive.
    //
    // Example:
    //
    // START 1
    // START 2
    // COMPLETE 1
    // START 3
    // COMPLETE 2
    // COMPLETE 3

    private Queue<QueuedRequest> requestQueue =
        new Queue<QueuedRequest>();

    // Keeps attempts available so COMPLETE can find
    // the correct row later.
    private List<AttemptData> activeAttempts =
        new List<AttemptData>();

    private bool isProcessing = false;

    // =========================================================
    // UNITY SETUP
    // =========================================================

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

    // =========================================================
    // START ATTEMPT
    // =========================================================

    public void StartAttempt(
        string url,
        string participantID,
        int attempts)
    {
        AttemptData data = new AttemptData();

        data.url = url;
        data.participantID = participantID;
        data.attempts = attempts;

        // Keep this attempt available for COMPLETE.
        activeAttempts.Add(data);

        // Add START request.
        QueuedRequest request = new QueuedRequest();

        request.type = RequestType.Start;
        request.attempt = data;

        requestQueue.Enqueue(request);

        Debug.Log(
            "[QUEUE] Added START for attempt "
            + attempts
            + ". Queue size: "
            + requestQueue.Count
        );

        ProcessQueue();
    }

    // =========================================================
    // COMPLETE ATTEMPT
    // =========================================================

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

        // Find the corresponding attempt.
        foreach (AttemptData data in activeAttempts)
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
                "[QUEUE] Could not find attempt "
                + attempts
                + " for COMPLETE."
            );

            return;
        }

        // Store completion data.
        target.score = score;
        target.totalQuestions = totalQuestions;
        target.correctAnswers = correctAnswers;
        target.accuracy = accuracy;
        target.completionTime = completionTime;
        target.gamificationUsed = gamificationUsed;

        target.completeQueued = true;

        // Add COMPLETE request.
        QueuedRequest request = new QueuedRequest();

        request.type = RequestType.Complete;
        request.attempt = target;

        requestQueue.Enqueue(request);

        Debug.Log(
            "[QUEUE] Added COMPLETE for attempt "
            + attempts
            + ". Queue size: "
            + requestQueue.Count
        );

        ProcessQueue();
    }

    // =========================================================
    // PROCESS QUEUE
    // =========================================================

    private void ProcessQueue()
    {
        if (isProcessing)
            return;

        if (requestQueue.Count == 0)
            return;

        isProcessing = true;

        StartCoroutine(ProcessNextRequest());
    }

    private IEnumerator ProcessNextRequest()
    {
        // Take the FIRST request.
        QueuedRequest request =
            requestQueue.Dequeue();

        AttemptData data = request.attempt;

        // =====================================================
        // START
        // =====================================================

        if (request.type == RequestType.Start)
        {
            yield return StartCoroutine(
                SendStart(data)
            );

            if (!data.startFinished)
            {
                data.startFailed = true;

                Debug.LogError(
                    "[QUEUE] START failed permanently for attempt "
                    + data.attempts
                );

                // If a COMPLETE request already exists for this
                // attempt, it cannot succeed.
                //
                // We remove the attempt from active tracking.
                activeAttempts.Remove(data);
            }
        }

        // =====================================================
        // COMPLETE
        // =====================================================

        else if (request.type == RequestType.Complete)
        {
            // -------------------------------------------------
            // IMPORTANT:
            //
            // COMPLETE may arrive while START is still
            // communicating with Apps Script.
            //
            // Instead of failing, wait for START.
            // -------------------------------------------------

            if (!data.startFinished)
            {
                Debug.Log(
                    "[QUEUE] COMPLETE waiting for START "
                    + "to finish for attempt "
                    + data.attempts
                );

                // Wait until START finishes or fails.
                while (!data.startFinished &&
                       !data.startFailed)
                {
                    yield return null;
                }
            }

            // START failed permanently.
            if (data.startFailed)
            {
                Debug.LogError(
                    "[QUEUE] Cannot COMPLETE attempt "
                    + data.attempts
                    + " because START failed."
                );

                activeAttempts.Remove(data);
            }
            else
            {
                // START succeeded.
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
                        "[QUEUE] Attempt "
                        + data.attempts
                        + " completed successfully."
                    );
                }
                else
                {
                    Debug.LogError(
                        "[QUEUE] COMPLETE failed permanently "
                        + "for attempt "
                        + data.attempts
                    );
                }

                // Attempt is finished.
                activeAttempts.Remove(data);
            }
        }

        isProcessing = false;

        // Process next request.
        if (requestQueue.Count > 0)
        {
            StartCoroutine(StartNextProcess());
        }
    }

    // =========================================================
    // START NEXT REQUEST
    // =========================================================

    private IEnumerator StartNextProcess()
    {
        // Wait one frame.
        yield return null;

        ProcessQueue();
    }

    // =========================================================
    // SEND START
    // =========================================================

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

        for (int retry = 0;
             retry <= MAX_RETRIES;
             retry++)
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

                StartAttemptResponse result = null;

                try
                {
                    result =
                        JsonUtility.FromJson<StartAttemptResponse>(
                            response
                        );
                }
                catch
                {
                    Debug.LogError(
                        "[UPLOAD] Could not parse START response."
                    );
                }

                if (result != null &&
                    result.success)
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

        data.startFailed = true;
    }

    // =========================================================
    // SEND COMPLETE
    // =========================================================

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

        for (int retry = 0;
             retry <= MAX_RETRIES;
             retry++)
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

                    if (result != null)
                    {
                        googleSuccess = result.success;
                    }
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
        );

        callback(false);
    }

    // =========================================================
    // RESPONSE CLASS
    // =========================================================

    [System.Serializable]
    private class StartAttemptResponse
    {
        public bool success;
        public int row;
    }
}
