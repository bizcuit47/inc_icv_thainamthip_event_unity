using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using Increstive.CommonUnity.Utility;

public class TextApiController : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("Web address that returns the texts as JSON, oldest first, in the form { \"items\": [ { \"_id\": \"...\", \"text\": \"...\" } ] }.")]
    private string apiUrl;

    [SerializeField]
    [HelpButton("How many seconds to wait between two requests to the web address.")]
    private float pollSeconds = 1f;

    [SerializeField]
    [HelpButton("The display that shows each new text. A new text replaces the one on screen.")]
    private TextDisplayBase textDisplay;

    [SerializeField]
    [HelpButton("When on, old texts are shown again one by one while no new text arrives.")]
    private bool isIdleEnabled = true;

    [SerializeField]
    [HelpButton("How many seconds without a new text before idle mode starts.")]
    private float idleAfterSeconds = 180f;

    [SerializeField]
    [HelpButton("In idle mode, on: pick a random old text each time. Off: go through the old texts in order.")]
    private bool isIdleRandomPick = true;

    [SerializeField]
    [HelpButton("In idle mode, how many seconds to wait before showing the next old text.")]
    private float idleIntervalSeconds = 8f;

    [SerializeField]
    [HelpButton("How many of the latest texts are remembered for idle mode.")]
    private int historyLimit = 10;

    // MARK: Private
    private readonly HashSet<string> seenIds = new HashSet<string>();
    private readonly List<string> history = new List<string>();
    private Coroutine pollCoroutine;
    private Coroutine idleCoroutine;
    private bool isBootstrapped;
    private float activityTime;
    private int idleIndex = -1;

    // MARK: Lifecycle
    private void OnEnable()
    {
        isBootstrapped = false;
        activityTime = Time.unscaledTime;
        idleIndex = -1;
        seenIds.Clear();
        history.Clear();
        CoroutineUtils.Start(this, ref pollCoroutine, PollRoutine());
        CoroutineUtils.Start(this, ref idleCoroutine, IdleRoutine());
    }

    private void OnDisable()
    {
        StopRoutine(ref pollCoroutine);
        StopRoutine(ref idleCoroutine);
    }

    private void OnValidate()
    {
        pollSeconds = Mathf.Max(0.1f, pollSeconds);
        idleAfterSeconds = Mathf.Max(0f, idleAfterSeconds);
        idleIntervalSeconds = Mathf.Max(0.5f, idleIntervalSeconds);
        historyLimit = Mathf.Max(1, historyLimit);
    }

    // MARK: Private
    private void StopRoutine(ref Coroutine coroutine)
    {
        if (coroutine == null)
            return;

        StopCoroutine(coroutine);
        coroutine = null;
    }

    private IEnumerator PollRoutine()
    {
        WaitForSecondsRealtime pollWait = new WaitForSecondsRealtime(pollSeconds);
        for (; ; )
        {
            yield return FetchRoutine();
            yield return pollWait;
        }
    }

    private IEnumerator FetchRoutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("TextApiController request failed: " + request.error);
                yield break;
            }

            HandleResponse(JsonUtility.FromJson<TextApiResponse>(request.downloadHandler.text));
        }
    }

    private void HandleResponse(TextApiResponse response)
    {
        bool isSeedOnly = !isBootstrapped;
        isBootstrapped = true;
        if (response == null || response.items == null)
            return;

        string textLatest = null;
        for (int i = 0; i < response.items.Length; i++)
        {
            TextApiItem item = response.items[i];
            if (string.IsNullOrEmpty(item._id) || string.IsNullOrEmpty(item.text))
                continue;

            if (!seenIds.Add(item._id))
                continue;

            AddHistory(item.text);
            textLatest = item.text;
        }

        if (isSeedOnly || textLatest == null)
            return;

        activityTime = Time.unscaledTime;
        textDisplay.Show(textLatest);
    }

    private void AddHistory(string text)
    {
        history.Add(text);
        if (history.Count > historyLimit)
            history.RemoveAt(0);
    }

    private IEnumerator IdleRoutine()
    {
        WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
        WaitForSecondsRealtime intervalWait = new WaitForSecondsRealtime(idleIntervalSeconds);
        for (; ; )
        {
            bool isIdle = isIdleEnabled
                && history.Count > 0
                && Time.unscaledTime - activityTime >= idleAfterSeconds;
            if (!isIdle)
            {
                yield return endOfFrameWait;
                continue;
            }

            idleIndex = GetNextIdleIndex();
            textDisplay.Show(history[idleIndex]);
            yield return intervalWait;
        }
    }

    private int GetNextIdleIndex()
    {
        if (!isIdleRandomPick || history.Count == 1)
            return (idleIndex + 1) % history.Count;

        if (idleIndex < 0)
            return Random.Range(0, history.Count);

        int indexNext = Random.Range(0, history.Count - 1);
        return indexNext >= idleIndex ? indexNext + 1 : indexNext;
    }
}
