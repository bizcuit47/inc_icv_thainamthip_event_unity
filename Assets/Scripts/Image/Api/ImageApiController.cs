using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using Increstive.CommonUnity.Utility;

public class ImageApiController : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("Web address that returns the images as JSON, in the form { \"items\": [ { \"_id\": \"...\", \"imageUrl\": \"...\", \"createdAt\": \"...\" } ] }. The imageUrl may be relative to this address.")]
    private string apiUrl;

    [SerializeField]
    [HelpButton("How many seconds to wait between two requests to the web address.")]
    private float pollSeconds = 1f;

    [SerializeField]
    [HelpButton("The slots that show each new image. Several new images can be on screen at the same time, one per slot.")]
    private ImageSlotsManager imageDisplay;

    [SerializeField]
    [HelpButton("When on, recent images are shown again one by one while no new image arrives.")]
    private bool isIdleEnabled = true;

    [SerializeField]
    [HelpButton("How many seconds without a new image before idle mode starts.")]
    private float idleAfterSeconds = 180f;

    [SerializeField]
    [HelpButton("In idle mode, how many seconds to wait before showing the next recent image.")]
    private float idleIntervalSeconds = 8f;

    [SerializeField]
    [HelpButton("In idle mode, on: pick a random recent image each time. Off: go through the recent images in order.")]
    private bool isIdleRandomPick = true;

    [SerializeField]
    [HelpButton("How many of the latest images are remembered for idle mode.")]
    private int historyLimit = 10;

    // MARK: Private
    private readonly HashSet<string> seenIds = new HashSet<string>();
    private readonly List<ImageApiItem> history = new List<ImageApiItem>();
    private readonly List<ImageApiItem> itemsNew = new List<ImageApiItem>();
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
        ImageApiResponse response = null;
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("ImageApiController request failed: " + request.error);
                yield break;
            }

            response = JsonUtility.FromJson<ImageApiResponse>(request.downloadHandler.text);
        }

        yield return HandleResponseRoutine(response);
    }

    private IEnumerator HandleResponseRoutine(ImageApiResponse response)
    {
        bool isSeedOnly = !isBootstrapped;
        isBootstrapped = true;
        if (response == null || response.items == null)
            yield break;

        itemsNew.Clear();
        for (int i = 0; i < response.items.Length; i++)
        {
            ImageApiItem item = response.items[i];
            if (item == null || string.IsNullOrEmpty(item._id) || string.IsNullOrEmpty(item.imageUrl))
                continue;

            if (!seenIds.Add(item._id))
                continue;

            AddHistory(item);
            itemsNew.Add(item);
        }

        if (isSeedOnly || itemsNew.Count == 0)
            yield break;

        activityTime = Time.unscaledTime;
        itemsNew.Sort(CompareByCreatedAt);
        int firstIndex = Mathf.Max(0, itemsNew.Count - imageDisplay.SlotCount);
        for (int i = firstIndex; i < itemsNew.Count; i++)
            yield return DownloadRoutine(itemsNew[i], null);
    }

    private void AddHistory(ImageApiItem item)
    {
        history.Add(item);
        history.Sort(CompareByCreatedAt);
        if (history.Count > historyLimit)
            history.RemoveAt(0);
    }

    private static int CompareByCreatedAt(ImageApiItem a, ImageApiItem b)
    {
        return string.CompareOrdinal(a.createdAt, b.createdAt);
    }

    private IEnumerator DownloadRoutine(ImageApiItem item, Func<bool> canShow)
    {
        if (!Uri.TryCreate(new Uri(apiUrl), item.imageUrl, out Uri imageUri))
            yield break;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUri))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("ImageApiController download failed: " + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (canShow != null && !canShow())
            {
                Destroy(texture);
                yield break;
            }

            imageDisplay.Show(texture);
        }
    }

    private bool IsIdleReady()
    {
        return isIdleEnabled
            && history.Count > 0
            && Time.unscaledTime - activityTime >= idleAfterSeconds;
    }

    private IEnumerator IdleRoutine()
    {
        WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
        WaitForSecondsRealtime intervalWait = new WaitForSecondsRealtime(idleIntervalSeconds);
        for (; ; )
        {
            if (!IsIdleReady())
            {
                yield return endOfFrameWait;
                continue;
            }

            idleIndex = GetNextIdleIndex();
            yield return DownloadRoutine(history[idleIndex], IsIdleReady);
            yield return intervalWait;
        }
    }

    private int GetNextIdleIndex()
    {
        if (!isIdleRandomPick || history.Count == 1)
            return (idleIndex + 1) % history.Count;

        if (idleIndex < 0 || idleIndex >= history.Count)
            return UnityEngine.Random.Range(0, history.Count);

        int indexNext = UnityEngine.Random.Range(0, history.Count - 1);
        return indexNext >= idleIndex ? indexNext + 1 : indexNext;
    }
}
