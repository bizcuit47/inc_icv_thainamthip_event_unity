using System.Collections.Generic;
using UnityEngine;

public class ImageSlotsManager : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("The settings asset that says where images sit for 1, 2, 3 and 4 images on screen.")]
    private ImageLayoutConfig layoutConfig;

    [SerializeField]
    [HelpButton("The slots where images appear, one image per slot. A new image uses an empty slot first. When all slots are busy it replaces the image that has been on screen the longest.")]
    private ImageSlotController[] slots;

    [SerializeField]
    [HelpButton("When on, an image never fades away on its own. It stays until a new image replaces it. When off, each slot uses its own Stay Seconds.")]
    private bool isStayUntilReplaced;

    // MARK: Private
    private readonly List<ImageSlotController> slotsActive = new List<ImageSlotController>();

    // MARK: Getters/Setters
    public int SlotCount => slots.Length;

    // MARK: Lifecycle
    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Configure(this);
    }

    private void OnDisable()
    {
        Clear();
    }

    // MARK: Public
    public void Show(Texture2D texture)
    {
        if (texture == null || slots.Length == 0)
        {
            Destroy(texture);
            return;
        }

        ImageSlotController slotFree = FindFreeSlot();
        if (slotFree == null)
        {
            FindOldestSlot().Show(texture, isStayUntilReplaced);
            return;
        }

        CollectActiveSlots();
        slotsActive.Add(slotFree);
        ApplyLayout(slotFree);
        slotFree.Show(texture, isStayUntilReplaced);
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Clear();
    }

    public void NotifySlotFinished(ImageSlotController slot)
    {
        CollectActiveSlots();
        ApplyLayout(null);
    }

    // MARK: Private
    private ImageSlotController FindFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsShowing)
                return slots[i];
        }

        return null;
    }

    private ImageSlotController FindOldestSlot()
    {
        ImageSlotController slotOldest = slots[0];
        for (int i = 1; i < slots.Length; i++)
        {
            if (slots[i].ShownTime < slotOldest.ShownTime)
                slotOldest = slots[i];
        }

        return slotOldest;
    }

    private void CollectActiveSlots()
    {
        slotsActive.Clear();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsShowing)
                slotsActive.Add(slots[i]);
        }

        slotsActive.Sort(CompareByPositionIndex);
    }

    private static int CompareByPositionIndex(ImageSlotController a, ImageSlotController b)
    {
        return a.PositionIndex.CompareTo(b.PositionIndex);
    }

    private void ApplyLayout(ImageSlotController slotNew)
    {
        Vector2[] positions = layoutConfig.GetPositions(slotsActive.Count);
        for (int i = 0; i < slotsActive.Count; i++)
        {
            ImageSlotController slot = slotsActive[i];
            slot.SetPositionIndex(i);
            Vector2 position = positions[Mathf.Min(i, positions.Length - 1)];
            if (slot == slotNew)
                slot.SnapTo(position);
            else
                slot.MoveTo(position, layoutConfig.MoveSeconds, layoutConfig.MoveCurve);
        }
    }
}
