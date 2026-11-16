using UnityEngine;

public abstract class TextDisplayBase : MonoBehaviour
{
    // MARK: Public
    public abstract void Show(string text);

    public abstract void Clear();
}
