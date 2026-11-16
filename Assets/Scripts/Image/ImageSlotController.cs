using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Increstive.CommonUnity.Utility;

public class ImageSlotController : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("The image shown in this slot. Anchor and pivot must be centered. The slots manager moves this object to the right place.")]
    private RawImage rawImage;

    [SerializeField]
    [HelpButton("Controls the transparency of the image.")]
    private CanvasGroup canvasGroup;

    [SerializeField]
    [HelpButton("The largest size in pixels the image may take. The image is scaled to fit inside while keeping its shape.")]
    private Vector2 maxSize = new Vector2(1200f, 800f);

    [SerializeField]
    [HelpButton("How long, in seconds, a new image takes to fade in.")]
    private float fadeInSeconds = 0.5f;

    [SerializeField]
    [HelpButton("How long, in seconds, the old image takes to fade out. A new image replaces the old one after this.")]
    private float fadeOutSeconds = 0.5f;

    [SerializeField]
    [HelpButton("How long, in seconds, an image stays fully visible before it fades away on its own. Ignored when Stay Until Replaced is on at the slots manager.")]
    private float staySeconds = 6f;

    [SerializeField]
    [HelpButton("Easing of the fade-in. The curve goes from 0 to 1 over the fade-in time.")]
    private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    [HelpButton("Easing of the fade-out. The curve goes from 0 to 1 over the fade-out time.")]
    private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // MARK: Private
    private readonly WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
    private RectTransform rawImageTransform;
    private Coroutine swapCoroutine;
    private Texture2D textureCurr;
    private Texture2D textureNext;
    private ImageSlotsManager managerCurr;
    private Coroutine moveCoroutine;
    private Vector2 positionStart;
    private Vector2 positionTarget;
    private float moveSecondsCurr;
    private AnimationCurve moveCurveCurr;
    private float shownTime;
    private int positionIndex;
    private bool isStayUntilReplacedCurr;

    // MARK: Getters/Setters
    public bool IsShowing => swapCoroutine != null || textureCurr != null;
    public float ShownTime => shownTime;
    public int PositionIndex => positionIndex;

    // MARK: Lifecycle
    private void Awake()
    {
        rawImageTransform = rawImage.rectTransform;
        canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnValidate()
    {
        maxSize = new Vector2(Mathf.Max(1f, maxSize.x), Mathf.Max(1f, maxSize.y));
        fadeInSeconds = Mathf.Max(0.01f, fadeInSeconds);
        fadeOutSeconds = Mathf.Max(0.01f, fadeOutSeconds);
        staySeconds = Mathf.Max(0f, staySeconds);
    }

    // MARK: Public
    public void Configure(ImageSlotsManager manager)
    {
        managerCurr = manager;
    }

    public void SetPositionIndex(int index)
    {
        positionIndex = index;
    }

    public void SnapTo(Vector2 position)
    {
        StopMove();
        rawImageTransform.anchoredPosition = position;
    }

    public void MoveTo(Vector2 position, float seconds, AnimationCurve curve)
    {
        positionStart = rawImageTransform.anchoredPosition;
        positionTarget = position;
        moveSecondsCurr = seconds;
        moveCurveCurr = curve;
        CoroutineUtils.Start(this, ref moveCoroutine, MoveRoutine());
    }

    public void Show(Texture2D texture, bool isStayUntilReplaced)
    {
        if (texture == null)
            return;

        DestroyTexture(ref textureNext);
        textureNext = texture;
        isStayUntilReplacedCurr = isStayUntilReplaced;
        shownTime = Time.unscaledTime;
        CoroutineUtils.Start(this, ref swapCoroutine, SwapRoutine());
    }

    public void Clear()
    {
        StopMove();
        if (swapCoroutine != null)
        {
            StopCoroutine(swapCoroutine);
            swapCoroutine = null;
        }

        canvasGroup.alpha = 0f;
        rawImage.texture = null;
        DestroyTexture(ref textureCurr);
        DestroyTexture(ref textureNext);
    }

    // MARK: Private
    private void StopMove()
    {
        if (moveCoroutine == null)
            return;

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }

    private IEnumerator MoveRoutine()
    {
        for (float elapsedSeconds = 0f; elapsedSeconds < moveSecondsCurr; elapsedSeconds += Time.unscaledDeltaTime)
        {
            float step = moveCurveCurr.Evaluate(elapsedSeconds / moveSecondsCurr);
            rawImageTransform.anchoredPosition = Vector2.LerpUnclamped(positionStart, positionTarget, step);
            yield return endOfFrameWait;
        }

        rawImageTransform.anchoredPosition = positionTarget;
        moveCoroutine = null;
    }

    private void DestroyTexture(ref Texture2D texture)
    {
        if (texture == null)
            return;

        Destroy(texture);
        texture = null;
    }

    private void ApplyTexture(Texture2D texture)
    {
        rawImage.texture = texture;
        float fitScale = Mathf.Min(maxSize.x / texture.width, maxSize.y / texture.height);
        rawImageTransform.sizeDelta = new Vector2(texture.width, texture.height) * fitScale;
    }

    private IEnumerator SwapRoutine()
    {
        if (canvasGroup.alpha > 0f)
            yield return FadeRoutine(0f, fadeOutSeconds * canvasGroup.alpha, fadeOutCurve);

        rawImage.texture = null;
        DestroyTexture(ref textureCurr);
        textureCurr = textureNext;
        textureNext = null;
        ApplyTexture(textureCurr);
        yield return FadeRoutine(1f, fadeInSeconds, fadeInCurve);
        if (isStayUntilReplacedCurr)
        {
            swapCoroutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(staySeconds);
        yield return FadeRoutine(0f, fadeOutSeconds, fadeOutCurve);
        rawImage.texture = null;
        DestroyTexture(ref textureCurr);
        swapCoroutine = null;
        managerCurr.NotifySlotFinished(this);
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
