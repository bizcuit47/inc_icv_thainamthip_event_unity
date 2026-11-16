using UnityEngine;

public class LanePlacement : ITextPlacement
{
    // MARK: Private
    private readonly TextDisplayConfig config;
    private float[] laneNextFreeSeconds = new float[0];
    private Vector2 areaSize;

    // MARK: Lifecycle
    public LanePlacement(TextDisplayConfig config)
    {
        this.config = config;
    }

    // MARK: Public
    public void SetArea(Vector2 areaSize)
    {
        this.areaSize = areaSize;
    }

    public bool TryReserve(Vector2 size, float nowSeconds, out TextPlacementSlot slot)
    {
        slot = default;
        slot.laneIndex = -1;
        EnsureLanes();

        float laneHeight = GetLaneHeight();
        if (laneHeight <= 0f || size.x <= 0f || size.y <= 0f)
            return false;

        int laneIndex = FindFreeLane(nowSeconds);
        if (laneIndex < 0)
            return false;

        float fitScale = size.y > laneHeight ? laneHeight / size.y : 1f;
        Vector2 scaledSize = size * fitScale;
        float direction = config.AnimationStyle == TextAnimationStyle.SlideInLeft ? 1f : -1f;
        float startX = -direction * (areaSize.x * 0.5f + scaledSize.x * 0.5f);
        float positionY = GetLaneCenterY(laneIndex, laneHeight) + GetAlignmentOffsetY(laneHeight, scaledSize.y);

        slot.restPosition = new Vector2(startX, positionY);
        slot.reservedRect = new Rect(startX - scaledSize.x * 0.5f, positionY - scaledSize.y * 0.5f, scaledSize.x, scaledSize.y);
        slot.laneIndex = laneIndex;
        slot.slideDirection = direction;
        slot.fitScale = fitScale;
        slot.staySeconds = Mathf.Max(0f, areaSize.x / config.SlideSpeed - config.InSeconds);
        laneNextFreeSeconds[laneIndex] = nowSeconds + (scaledSize.x + config.LaneItemSpacing) / config.SlideSpeed;
        return true;
    }

    public void Release(TextPlacementSlot slot) { }

    public void Clear()
    {
        for (int i = 0; i < laneNextFreeSeconds.Length; i++)
            laneNextFreeSeconds[i] = float.NegativeInfinity;
    }

    // MARK: Private
    private void EnsureLanes()
    {
        if (laneNextFreeSeconds.Length == config.LaneCount)
            return;

        laneNextFreeSeconds = new float[config.LaneCount];
        Clear();
    }

    private float GetLaneHeight()
    {
        int laneCount = config.LaneCount;
        return (areaSize.y - (laneCount - 1) * config.LaneSpacing) / laneCount;
    }

    private float GetLaneCenterY(int laneIndex, float laneHeight)
    {
        return areaSize.y * 0.5f - laneHeight * 0.5f - laneIndex * (laneHeight + config.LaneSpacing);
    }

    private float GetAlignmentOffsetY(float laneHeight, float itemHeight)
    {
        float slack = (laneHeight - itemHeight) * 0.5f;
        if (config.LaneAlignment == TextLaneAlignment.Start)
            return slack;
        if (config.LaneAlignment == TextLaneAlignment.End)
            return -slack;
        return 0f;
    }

    private int FindFreeLane(float nowSeconds)
    {
        int laneCount = laneNextFreeSeconds.Length;
        int startIndex = config.LaneOrder == TextLaneOrder.Random ? Random.Range(0, laneCount) : 0;
        for (int i = 0; i < laneCount; i++)
        {
            int laneIndex = (startIndex + i) % laneCount;
            if (laneNextFreeSeconds[laneIndex] <= nowSeconds)
                return laneIndex;
        }

        return -1;
    }
}
