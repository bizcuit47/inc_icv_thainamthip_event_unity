using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Increstive.CommonUnity.Utility;

public class TextDisplayManager : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("The settings asset that controls animation, layout and capacity.")]
    private TextDisplayConfig config;

    [SerializeField]
    [HelpButton("The prefab used for every shown text. Items are reused instead of being created again.")]
    private TextItemController itemPrefab;

    [SerializeField]
    [HelpButton("The area where texts appear. Texts become children of this object.")]
    private RectTransform areaTransform;

    // MARK: Private
    private readonly List<TextItemController> items = new List<TextItemController>();
    private readonly Queue<string> pendingTexts = new Queue<string>();
    private readonly WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
    private ITextPlacement placement;
    private Coroutine drainQueueCoroutine;

    // MARK: Lifecycle
    private void Awake()
    {
        if (config.AnimationStyle.IsSlide())
            placement = new LanePlacement(config);
        else
            placement = new ScatterPlacement(config);
    }

    private void OnDisable()
    {
        Clear();
    }

    // MARK: Public
    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (pendingTexts.Count == 0 && TryShowText(text))
            return;

        EnqueueText(text);
    }

    public void Clear()
    {
        StopDrainQueue();
        pendingTexts.Clear();
        for (int i = 0; i < items.Count; i++)
            items[i].ForceHide();

        if (placement != null)
            placement.Clear();
    }

    public void NotifyItemFinished(TextItemController item)
    {
        placement.Release(item.Slot);
        item.ForceHide();
    }

    // MARK: Private
    private bool TryShowText(string text)
    {
        TextItemController item = AcquireItem();
        if (item == null)
            return false;

        Vector2 size = item.Prepare(config, text);
        placement.SetArea(areaTransform.rect.size);
        if (!placement.TryReserve(size, Time.unscaledTime, out TextPlacementSlot slot))
        {
            item.ForceHide();
            return false;
        }

        item.Play(this, slot);
        return true;
    }

    private TextItemController AcquireItem()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i].IsInUse)
                return items[i];
        }

        if (items.Count >= config.MaxActiveItems)
            return null;

        TextItemController item = Instantiate(itemPrefab, areaTransform);
        item.ForceHide();
        items.Add(item);
        return item;
    }

    private void EnqueueText(string text)
    {
        if (config.OverflowPolicy == TextOverflowPolicy.Drop || config.MaxQueueLength <= 0)
            return;

        while (pendingTexts.Count >= config.MaxQueueLength)
            pendingTexts.Dequeue();

        pendingTexts.Enqueue(text);
        if (drainQueueCoroutine == null)
            CoroutineUtils.Start(this, ref drainQueueCoroutine, DrainQueueRoutine());
    }

    private void StopDrainQueue()
    {
        if (drainQueueCoroutine == null)
            return;

        StopCoroutine(drainQueueCoroutine);
        drainQueueCoroutine = null;
    }

    private IEnumerator DrainQueueRoutine()
    {
        while (pendingTexts.Count > 0)
        {
            if (TryShowText(pendingTexts.Peek()))
                pendingTexts.Dequeue();

            yield return endOfFrameWait;
        }

        drainQueueCoroutine = null;
    }
}
