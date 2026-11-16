using UnityEngine;

public interface ITextPlacement
{
    void SetArea(Vector2 areaSize);

    bool TryReserve(Vector2 size, float nowSeconds, out TextPlacementSlot slot);

    void Release(TextPlacementSlot slot);

    void Clear();
}
