using UnityEngine;

public class HelpButtonAttribute : PropertyAttribute
{
    // MARK: Public
    public readonly string text;

    // MARK: Lifecycle
    public HelpButtonAttribute(string text)
    {
        this.text = text;
    }
}
