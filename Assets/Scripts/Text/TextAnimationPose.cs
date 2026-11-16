using UnityEngine;

public static class TextAnimationPose
{
    // MARK: Public
    public static void Evaluate(
        TextDisplayConfig config,
        TextPlacementSlot slot,
        TextAnimationPhase phase,
        float step,
        float elapsedSeconds,
        out Vector2 offset,
        out float alpha,
        out float scale
    )
    {
        float inEased = config.InCurve.Evaluate(step);
        float outEased = config.OutCurve.Evaluate(step);
        alpha = GetAlpha(phase, inEased, outEased);
        offset = Vector2.zero;
        scale = slot.fitScale;

        if (config.AnimationStyle.IsSlide())
        {
            offset.x = slot.slideDirection * config.SlideSpeed * elapsedSeconds;
            return;
        }

        if (config.AnimationStyle == TextAnimationStyle.FloatUp)
        {
            offset.y = GetFloatOffsetY(config, phase, inEased, outEased);
            return;
        }

        if (phase == TextAnimationPhase.In)
            scale *= Mathf.LerpUnclamped(config.FadeScaleFrom, 1f, inEased);
    }

    // MARK: Private
    private static float GetAlpha(TextAnimationPhase phase, float inEased, float outEased)
    {
        if (phase == TextAnimationPhase.In)
            return Mathf.Clamp01(inEased);
        if (phase == TextAnimationPhase.Out)
            return Mathf.Clamp01(1f - outEased);
        return 1f;
    }

    private static float GetFloatOffsetY(
        TextDisplayConfig config,
        TextAnimationPhase phase,
        float inEased,
        float outEased
    )
    {
        float halfDistance = config.FloatDistance * 0.5f;
        if (phase == TextAnimationPhase.In)
            return Mathf.LerpUnclamped(-halfDistance, 0f, inEased);
        if (phase == TextAnimationPhase.Out)
            return Mathf.LerpUnclamped(0f, halfDistance, outEased);
        return 0f;
    }
}
