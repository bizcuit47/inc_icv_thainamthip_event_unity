using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using Increstive.CommonUnity.Utility;

public class TextApiController : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("Web address that returns the texts as JSON, in the form { \"items\": [ { \"_id\": \"...\", \"text\": \"...\" } ] }.")]
    private string apiUrl;

    [SerializeField]
    [HelpButton("How many seconds to wait between two requests to the web address.")]
    private float pollSeconds = 1f;

    [SerializeField]
    [HelpButton("The display that shows each new text.")]
    private TextDisplayManager textManager;

    // MARK: Private
    private readonly HashSet<string> seenIds = new HashSet<string>();
    private readonly WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
    private Coroutine pollCoroutine;
    private bool isBootstrapped;

    // MARK: Lifecycle
    private void OnEnable()
    {
        isBootstrapped = false;
        seenIds.Clear();
        CoroutineUtils.Start(this, ref pollCoroutine, PollRoutine());
    }

    private void OnDisable()
    {
        if (pollCoroutine == null)
            return;

        StopCoroutine(pollCoroutine);
        pollCoroutine = null;
    }

    private void OnValidate()
    {
        pollSeconds = Mathf.Max(0.1f, pollSeconds);
    }

    // MARK: Private
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

            TextApiResponse response = JsonUtility.FromJson<TextApiResponse>(request.downloadHandler.text);
            yield return HandleResponseRoutine(response);
        }
    }

    private IEnumerator HandleResponseRoutine(TextApiResponse response)
    {
        bool isSeedOnly = !isBootstrapped;
        isBootstrapped = true;
        if (response == null || response.items == null)
            yield break;

        for (int i = 0; i < response.items.Length; i++)
        {
            TextApiItem item = response.items[i];
            if (string.IsNullOrEmpty(item._id) || string.IsNullOrEmpty(item.text))
                continue;

            if (!seenIds.Add(item._id) || isSeedOnly)
                continue;

            textManager.Show(item.text);
            yield return endOfFrameWait;
        }
    }
}
