public enum TextAnimationStyle
{
    FloatUp,
    FadeIn,
    SlideInLeft,
    SlideInRight,
}

public enum TextOverflowPolicy
{
    Queue,
    Drop,
}

public enum TextLaneAlignment
{
    Start,
    Center,
    End,
}

public enum TextLaneOrder
{
    TopDown,
    Random,
}

public enum TextAnimationPhase
{
    In,
    Stay,
    Out,
}

public static class TextAnimationStyleExtension
{
    // MARK: Public
    public static bool IsSlide(this TextAnimationStyle style)
    {
        return style == TextAnimationStyle.SlideInLeft || style == TextAnimationStyle.SlideInRight;
    }
}
