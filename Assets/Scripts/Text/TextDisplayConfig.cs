using UnityEngine;

[CreateAssetMenu(fileName = "TextDisplayConfig", menuName = "Text Display/Config")]
public class TextDisplayConfig : ScriptableObject
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("Which animation each text uses. FloatUp and FadeIn place texts at random free spots. SlideInLeft and SlideInRight move texts across the screen in rows.")]
    private TextAnimationStyle animationStyle = TextAnimationStyle.FloatUp;

    [SerializeField]
    [HelpButton("How long, in seconds, the text takes to appear.")]
    private float inSeconds = 0.6f;

    [SerializeField]
    [HelpButton("How long, in seconds, the text stays fully visible. Ignored by the slide styles, which use Slide Speed instead.")]
    private float staySeconds = 3f;

    [SerializeField]
    [HelpButton("How long, in seconds, the text takes to fade away.")]
    private float outSeconds = 0.6f;

    [SerializeField]
    [HelpButton("Easing of the appear animation. The curve goes from 0 to 1 over the appear time.")]
    private AnimationCurve inCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    [HelpButton("Easing of the fade-away animation. The curve goes from 0 to 1 over the fade-away time.")]
    private AnimationCurve outCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    [HelpButton("FloatUp only: total distance in pixels the text drifts upward during its life.")]
    private float floatDistance = 120f;

    [SerializeField]
    [HelpButton("Slide styles only: how fast the text travels across the screen, in pixels per second.")]
    private float slideSpeed = 200f;

    [SerializeField]
    [HelpButton("Slide styles only: minimum empty space in pixels between two texts that follow each other in the same row.")]
    private float laneItemSpacing = 80f;

    [SerializeField]
    [HelpButton("FadeIn only: the text starts at this size multiplier and grows or shrinks to normal size. 1 means no size change.")]
    private float fadeScaleFrom = 0.8f;

    [SerializeField]
    [HelpButton("Font size of the text in points.")]
    private float fontSize = 48f;

    [SerializeField]
    [HelpButton("Maximum width in pixels before the text wraps onto a new line. Thai has no spaces, so this width controls where lines break.")]
    private float maxTextWidth = 800f;

    [SerializeField]
    [HelpButton("FloatUp and FadeIn only: minimum empty space in pixels kept between texts so they never overlap.")]
    private float spacing = 40f;

    [SerializeField]
    [HelpButton("FloatUp and FadeIn only: distance in pixels kept free between texts and the edge of the display area.")]
    private float edgePadding = 30f;

    [SerializeField]
    [HelpButton("FloatUp and FadeIn only: how many random positions are tried before giving up for now. Higher values pack texts tighter but cost more.")]
    private int maxPlacementTries = 50;

    [SerializeField]
    [HelpButton("Slide styles only: number of horizontal rows the display area is split into.")]
    private int laneCount = 5;

    [SerializeField]
    [HelpButton("Slide styles only: vertical gap in pixels between rows.")]
    private float laneSpacing = 20f;

    [SerializeField]
    [HelpButton("Slide styles only: where a text sits inside its row when it is shorter than the row. Start is the top, End is the bottom.")]
    private TextLaneAlignment laneAlignment = TextLaneAlignment.Center;

    [SerializeField]
    [HelpButton("Slide styles only: TopDown fills rows from the top. Random picks any free row.")]
    private TextLaneOrder laneOrder = TextLaneOrder.TopDown;

    [SerializeField]
    [HelpButton("Maximum number of texts shown at the same time.")]
    private int maxActiveItems = 30;

    [SerializeField]
    [HelpButton("What happens to a new text when there is no room: Queue keeps it until space is free, Drop discards it.")]
    private TextOverflowPolicy overflowPolicy = TextOverflowPolicy.Queue;

    [SerializeField]
    [HelpButton("Maximum number of waiting texts when Overflow Policy is Queue. When full, the oldest waiting text is discarded.")]
    private int maxQueueLength = 20;

    // MARK: Getters/Setters
    public TextAnimationStyle AnimationStyle => animationStyle;
    public float InSeconds => inSeconds;
    public float StaySeconds => staySeconds;
    public float OutSeconds => outSeconds;
    public AnimationCurve InCurve => inCurve;
    public AnimationCurve OutCurve => outCurve;
    public float FloatDistance => floatDistance;
    public float SlideSpeed => slideSpeed;
    public float LaneItemSpacing => laneItemSpacing;
    public float FadeScaleFrom => fadeScaleFrom;
    public float FontSize => fontSize;
    public float MaxTextWidth => maxTextWidth;
    public float Spacing => spacing;
    public float EdgePadding => edgePadding;
    public int MaxPlacementTries => maxPlacementTries;
    public int LaneCount => laneCount;
    public float LaneSpacing => laneSpacing;
    public TextLaneAlignment LaneAlignment => laneAlignment;
    public TextLaneOrder LaneOrder => laneOrder;
    public int MaxActiveItems => maxActiveItems;
    public TextOverflowPolicy OverflowPolicy => overflowPolicy;
    public int MaxQueueLength => maxQueueLength;

    // MARK: Lifecycle
    private void OnValidate()
    {
        inSeconds = Mathf.Max(0.01f, inSeconds);
        staySeconds = Mathf.Max(0f, staySeconds);
        outSeconds = Mathf.Max(0.01f, outSeconds);
        floatDistance = Mathf.Max(0f, floatDistance);
        slideSpeed = Mathf.Max(1f, slideSpeed);
        laneItemSpacing = Mathf.Max(0f, laneItemSpacing);
        fadeScaleFrom = Mathf.Max(0.1f, fadeScaleFrom);
        fontSize = Mathf.Max(1f, fontSize);
        maxTextWidth = Mathf.Max(1f, maxTextWidth);
        spacing = Mathf.Max(0f, spacing);
        edgePadding = Mathf.Max(0f, edgePadding);
        maxPlacementTries = Mathf.Max(1, maxPlacementTries);
        laneCount = Mathf.Max(1, laneCount);
        laneSpacing = Mathf.Max(0f, laneSpacing);
        maxActiveItems = Mathf.Max(1, maxActiveItems);
        maxQueueLength = Mathf.Max(0, maxQueueLength);
    }
}
