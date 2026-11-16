using System;

[Serializable]
public class TextApiItem
{
    // MARK: Public
    public string _id;
    public string text;
}

[Serializable]
public class TextApiResponse
{
    // MARK: Public
    public TextApiItem[] items;
}
