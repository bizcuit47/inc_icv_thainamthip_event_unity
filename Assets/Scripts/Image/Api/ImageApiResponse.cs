using System;

[Serializable]
public class ImageApiItem
{
    // MARK: Public
    public string _id;
    public string type;
    public string imageUrl;
    public string createdAt;
}

[Serializable]
public class ImageApiResponse
{
    // MARK: Public
    public ImageApiItem[] items;
}
