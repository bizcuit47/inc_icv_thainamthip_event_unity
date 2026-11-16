using System.Collections;
using UnityEngine;

using TMPro;

using Increstive.CommonUnity.Utility;

public class TextCenterController : TextDisplayBase
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("The TextMeshPro label shown in the middle of the screen. Word wrapping must be enabled on it.")]
    private TMP_Text label;

    [SerializeField]
    [HelpButton("Controls the transparency of the label.")]
    private CanvasGroup canvasGroup;

    [SerializeField]
    [HelpButton("How long, in seconds, a new text takes to fade in.")]
    private float fadeInSeconds = 0.5f;

    [SerializeField]
    [HelpButton("How long, in seconds, the old text takes to fade out. A new text replaces the old one after this.")]
    private float fadeOutSeconds = 0.5f;

    [SerializeField]
    [HelpButton("How long, in seconds, a text stays fully visible before it fades away on its own.")]
    private float staySeconds = 6f;

    [SerializeField]
    [HelpButton("Easing of the fade-in. The curve goes from 0 to 1 over the fade-in time.")]
    private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    [HelpButton("Easing of the fade-out. The curve goes from 0 to 1 over the fade-out time.")]
    private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // MARK: Private
    private readonly WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
    private Coroutine swapCoroutine;
    private string textNext;

    // MARK: Lifecycle
    private void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnValidate()
    {
        fadeInSeconds = Mathf.Max(0.01f, fadeInSeconds);
        fadeOutSeconds = Mathf.Max(0.01f, fadeOutSeconds);
        staySeconds = Mathf.Max(0f, staySeconds);
    }

    // MARK: Public
    public override void Show(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        textNext = text;
        CoroutineUtils.Start(this, ref swapCoroutine, SwapRoutine());
    }

    public override void Clear()
    {
        textNext = null;
        if (swapCoroutine != null)
        {
            StopCoroutine(swapCoroutine);
            swapCoroutine = null;
        }

        canvasGroup.alpha = 0f;
    }

    // MARK: Private
    private IEnumerator SwapRoutine()
    {
        if (canvasGroup.alpha > 0f)
            yield return FadeRoutine(0f, fadeOutSeconds * canvasGroup.alpha, fadeOutCurve);

        label.text = textNext;
        yield return FadeRoutine(1f, fadeInSeconds, fadeInCurve);
        yield return new WaitForSecondsRealtime(staySeconds);
        yield return FadeRoutine(0f, fadeOutSeconds, fadeOutCurve);
        swapCoroutine = null;
    }

    private IEnumerator FadeRoutine(float alphaTarget, float seconds, AnimationCurve curve)
    {
        float alphaStart = canvasGroup.alpha;
        for (float elapsedSeconds = 0f; elapsedSeconds < seconds; elapsedSeconds += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = Mathf.LerpUnclamped(alphaStart, alphaTarget, curve.Evaluate(elapsedSeconds / seconds));
            yield return endOfFrameWait;
        }

        canvasGroup.alpha = alphaTarget;
    }
}
