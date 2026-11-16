using System.Collections;
using UnityEngine;

using TMPro;

using Increstive.CommonUnity.Utility;

public class TextItemController : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("The TextMeshPro label that shows the text. Word wrapping must be enabled on it.")]
    private TMP_Text label;

    [SerializeField]
    [HelpButton("Controls the transparency of the whole item.")]
    private CanvasGroup canvasGroup;

    [SerializeField]
    [HelpButton("The RectTransform of this item. Anchor and pivot must be centered.")]
    private RectTransform rectTransform;

    // MARK: Private
    private readonly WaitForEndOfFrame endOfFrameWait = new WaitForEndOfFrame();
    private TextDisplayConfig configCurr;
    private TextDisplayManager managerCurr;
    private TextPlacementSlot slotCurr;
    private Coroutine lifetimeCoroutine;
    private bool isInUse;

    // MARK: Getters/Setters
    public bool IsInUse => isInUse;
    public TextPlacementSlot Slot => slotCurr;

    // MARK: Public
    public Vector2 Prepare(TextDisplayConfig config, string text)
    {
        StopLifetime();
        configCurr = config;
        isInUse = true;
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one;
        label.fontSize = config.FontSize;
        label.text = text;
        label.ForceMeshUpdate();
        Vector2 size = label.GetPreferredValues(text, config.MaxTextWidth, Mathf.Infinity);
        rectTransform.sizeDelta = size;
        return size;
    }

    public void Play(TextDisplayManager manager, TextPlacementSlot slot)
    {
        managerCurr = manager;
        slotCurr = slot;
        ApplyPose(0f);
        CoroutineUtils.Start(this, ref lifetimeCoroutine, LifetimeRoutine());
    }

    public void ForceHide()
    {
        StopLifetime();
        isInUse = false;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // MARK: Private
    private void StopLifetime()
    {
        if (lifetimeCoroutine == null)
            return;

        StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = null;
    }

    private IEnumerator LifetimeRoutine()
    {
        float totalSeconds = configCurr.InSeconds + slotCurr.staySeconds + configCurr.OutSeconds;
        for (float elapsedSeconds = 0f; elapsedSeconds < totalSeconds; elapsedSeconds += Time.unscaledDeltaTime)
        {
            ApplyPose(elapsedSeconds);
            yield return endOfFrameWait;
        }

        lifetimeCoroutine = null;
        managerCurr.NotifyItemFinished(this);
    }

    private void ApplyPose(float elapsedSeconds)
    {
        float inSeconds = configCurr.InSeconds;
        float outStartSeconds = inSeconds + slotCurr.staySeconds;
        TextAnimationPhase phase = TextAnimationPhase.Stay;
        float step = 1f;

        if (elapsedSeconds < inSeconds)
        {
            phase = TextAnimationPhase.In;
            step = elapsedSeconds / inSeconds;
        }
        else if (elapsedSeconds >= outStartSeconds)
        {
            phase = TextAnimationPhase.Out;
            step = Mathf.Clamp01((elapsedSeconds - outStartSeconds) / configCurr.OutSeconds);
        }

        TextAnimationPose.Evaluate(
            configCurr,
            slotCurr,
            phase,
            step,
            elapsedSeconds,
            out Vector2 offset,
            out float alpha,
            out float scale
        );
        rectTransform.anchoredPosition = slotCurr.restPosition + offset;
        rectTransform.localScale = new Vector3(scale, scale, 1f);
        canvasGroup.alpha = alpha;
    }
}
