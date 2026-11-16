using System.Collections.Generic;
using UnityEngine;

public class ScatterPlacement : ITextPlacement
{
    // MARK: Private
    private readonly TextDisplayConfig config;
    private readonly List<Rect> reservedRects = new List<Rect>();
    private Vector2 areaSize;

    // MARK: Lifecycle
    public ScatterPlacement(TextDisplayConfig config)
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
        slot.fitScale = 1f;
        slot.staySeconds = config.StaySeconds;

        Vector2 reservedSize = GetReservedSize(size);
        Vector2 halfSize = reservedSize * 0.5f;
        for (int tryIndex = 0; tryIndex < config.MaxPlacementTries; tryIndex++)
        {
            Vector2 position = GetRandomPosition(halfSize);
            Rect candidateRect = new Rect(position - halfSize, reservedSize);
            if (IsOverlapping(candidateRect))
                continue;

            reservedRects.Add(candidateRect);
            slot.restPosition = position;
            slot.reservedRect = candidateRect;
            return true;
        }

        return false;
    }

    public void Release(TextPlacementSlot slot)
    {
        int rectIndex = reservedRects.IndexOf(slot.reservedRect);
        if (rectIndex >= 0)
            reservedRects.RemoveAt(rectIndex);
    }

    public void Clear()
    {
        reservedRects.Clear();
    }

    // MARK: Private
    private Vector2 GetReservedSize(Vector2 size)
    {
        float scale = config.AnimationStyle == TextAnimationStyle.FadeIn ? Mathf.Max(1f, config.FadeScaleFrom) : 1f;
        float extraHeight = config.AnimationStyle == TextAnimationStyle.FloatUp ? config.FloatDistance : 0f;
        return new Vector2(size.x * scale + config.Spacing, size.y * scale + config.Spacing + extraHeight);
    }

    private Vector2 GetRandomPosition(Vector2 halfSize)
    {
        float minX = -areaSize.x * 0.5f + halfSize.x + config.EdgePadding;
        float maxX = areaSize.x * 0.5f - halfSize.x - config.EdgePadding;
        float minY = -areaSize.y * 0.5f + halfSize.y + config.EdgePadding;
        float maxY = areaSize.y * 0.5f - halfSize.y - config.EdgePadding;

        if (minX > maxX)
            minX = maxX = 0f;
        if (minY > maxY)
            minY = maxY = 0f;

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    private bool IsOverlapping(Rect candidateRect)
    {
        for (int i = 0; i < reservedRects.Count; i++)
        {
            if (candidateRect.Overlaps(reservedRects[i]))
                return true;
        }

        return false;
    }
}
