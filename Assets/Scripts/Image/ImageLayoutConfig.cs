using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageLayoutConfig", menuName = "Image Display/Layout Config")]
public class ImageLayoutConfig : ScriptableObject
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("Where images sit for each number of images on screen. Entry 1 is used when 1 image is shown, entry 2 when 2 are shown, and so on. Positions are in pixels from the screen center. The first position goes to the image that has been on screen the longest.")]
    private ImageLayoutPreset[] presets =
    {
        new ImageLayoutPreset
        {
            positions = new[] { new Vector2(0f, 0f) },
        },
        new ImageLayoutPreset
        {
            positions = new[] { new Vector2(-440f, 0f), new Vector2(440f, 0f) },
        },
        new ImageLayoutPreset
        {
            positions = new[] { new Vector2(-440f, -240f), new Vector2(440f, -240f), new Vector2(0f, 240f) },
        },
        new ImageLayoutPreset
        {
            positions = new[]
            {
                new Vector2(-440f, -240f),
                new Vector2(440f, -240f),
                new Vector2(-440f, 240f),
                new Vector2(440f, 240f),
            },
        },
    };

    [SerializeField]
    [HelpButton("How long, in seconds, an image takes to glide to its new position when the number of images on screen changes.")]
    private float moveSeconds = 0.5f;

    [SerializeField]
    [HelpButton("Easing of the glide. The curve goes from 0 to 1 over the glide time.")]
    private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // MARK: Getters/Setters
    public float MoveSeconds => moveSeconds;
    public AnimationCurve MoveCurve => moveCurve;

    // MARK: Public
    public Vector2[] GetPositions(int count)
    {
        int index = Mathf.Clamp(count, 1, presets.Length) - 1;
        return presets[index].positions;
    }

    // MARK: Lifecycle
    private void OnValidate()
    {
        moveSeconds = Mathf.Max(0.01f, moveSeconds);
    }
}

[Serializable]
public class ImageLayoutPreset
{
    // MARK: Public
    public Vector2[] positions;
}
