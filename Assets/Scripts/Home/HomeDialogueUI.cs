using UnityEngine;
using TMPro;

[System.Serializable]
public class HomeDialogueLine
{
    public UnityEngine.Events.UnityEvent onFinished = new UnityEngine.Events.UnityEvent();
    public Sprite portrait;
    public string speakerName;
    [TextArea(3, 8)] public string content;
}

public class HomeDialogueUI : MonoBehaviour
{
    public GameObject panel;
    public UnityEngine.UI.Image portraitImage;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public RectTransform continueArrow;
    public HomeDialogueLine preview = new HomeDialogueLine();
    [Header("Portrait layout")]
    [Min(0)] public float noPortraitTextShift = 640;
    [SerializeField] bool portraitLayoutConfigured;
    [SerializeField] Vector2 speakerPortraitPosition, dialoguePortraitPosition;
    [SerializeField] Vector2 speakerPortraitSize, dialoguePortraitSize;
    public void CapturePortraitLayout()
    {
        if (!speakerText || !dialogueText) return;
        speakerPortraitPosition = speakerText.rectTransform.anchoredPosition;
        dialoguePortraitPosition = dialogueText.rectTransform.anchoredPosition;
        speakerPortraitSize = speakerText.rectTransform.sizeDelta;
        dialoguePortraitSize = dialogueText.rectTransform.sizeDelta;
        portraitLayoutConfigured = true;
    }
    public void RefreshPreview() => Apply(preview);
    [Min(0)] public float charactersPerSecond = 35;
    [Header("Continue Arrow")]
    [Min(0.05f), Tooltip("한 번 켜졌다 꺼지는 전체 주기(초)")] public float arrowBlinkPeriod = 1f;
    [Min(0), Tooltip("텍스트 끝과 화살표 왼쪽 사이 간격(UI 단위)")] public float arrowTextGap = 24f;
    [Header("Monologue")]
    public Texture2D bubbleTexture;
    [Min(.1f)] public float monologueFontSize = 8;
    [System.NonSerialized] public HomeSpeechBubble bubbleView;
    System.Action completed;
    public bool IsSuspended { get; private set; }
    public bool IsOpen { get; private set; }
    float blinkElapsed;
    bool wasComplete;
    HomeDialogueLine[] lines;
    int index;
    int page = 1;
    public bool CanAdvance => IsOpen && !IsSuspended && (dialogueText.maxVisibleCharacters < CurrentPageEnd || page < dialogueText.textInfo.pageCount || index < lines.Length - 1);
    int CurrentPageEnd => dialogueText.textInfo.pageCount > 0 ? dialogueText.textInfo.pageInfo[Mathf.Clamp(page - 1, 0, dialogueText.textInfo.pageCount - 1)].lastCharacterIndex + 1 : dialogueText.textInfo.characterCount;
    float visible;
    void Awake()
    {
        if (dialogueText && !dialogueText.GetComponent<BitmapTextPixelScale>()) dialogueText.gameObject.AddComponent<BitmapTextPixelScale>();
        if (speakerText && !speakerText.GetComponent<BitmapTextPixelScale>()) speakerText.gameObject.AddComponent<BitmapTextPixelScale>();
        if (panel) panel.SetActive(false);
    }
    void OnValidate() { if (!Application.isPlaying && portraitLayoutConfigured && portraitImage && speakerText && dialogueText) Apply(preview); }
    void Apply(HomeDialogueLine line)
    {
        bool showPortrait = line.speakerName == "Jesus" && line.portrait;
        if (portraitImage)
        {
            portraitImage.sprite = showPortrait ? line.portrait : null;
            portraitImage.enabled = showPortrait;
            if (!portraitLayoutConfigured) CapturePortraitLayout();
            if (portraitLayoutConfigured)
            {
                float shift = showPortrait ? 0 : noPortraitTextShift;
                speakerText.rectTransform.anchoredPosition = speakerPortraitPosition + Vector2.left * (shift * (1 - speakerText.rectTransform.pivot.x));
                dialogueText.rectTransform.anchoredPosition = dialoguePortraitPosition + Vector2.left * (shift * (1 - dialogueText.rectTransform.pivot.x));
                speakerText.rectTransform.sizeDelta = speakerPortraitSize + Vector2.right * shift;
                dialogueText.rectTransform.sizeDelta = dialoguePortraitSize + Vector2.right * shift;
            }
        }
        if (speakerText) speakerText.text = line.speakerName;
        dialogueText.text = line.content;
    }
    public void Begin(HomeDialogueLine[] dialogue, System.Action onCompleted = null)
    {
        if (dialogue == null || dialogue.Length == 0) return;
        completed = onCompleted;
        lines = dialogue; index = 0; IsOpen = true; panel.SetActive(true); ShowLine();
    }
    void ShowLine()
    {
        Apply(lines[index]);
        page = 1;
        dialogueText.pageToDisplay = page;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.overflowMode = TextOverflowModes.Page;
        if (bubbleView) bubbleView.Fit(dialogueText);
        visible = 0; blinkElapsed = 0; wasComplete = false;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        if (continueArrow) continueArrow.gameObject.SetActive(false);
    }
    public void Advance()
    {
        if (!IsOpen || IsSuspended) return;
        dialogueText.ForceMeshUpdate();
        int end = CurrentPageEnd;
        if (dialogueText.maxVisibleCharacters < end)
        { visible = end; dialogueText.maxVisibleCharacters = end; return; }
        if (page < dialogueText.textInfo.pageCount)
        {
            page++;
            dialogueText.pageToDisplay = page;
            visible = dialogueText.textInfo.pageInfo[page - 1].firstCharacterIndex;
            dialogueText.maxVisibleCharacters = (int)visible;
            blinkElapsed = 0; wasComplete = false;
            return;
        }
        lines[index].onFinished?.Invoke();
        if (!IsSuspended) NextLine();
    }
    void NextLine()
    {
        if (++index < lines.Length) ShowLine(); else { var callback = completed; Close(); callback?.Invoke(); }
    }
    public void SetSuspended(bool suspended) { IsSuspended = suspended; panel.SetActive(!suspended && IsOpen); }
    public void ResumeAfterEvent() { if (!IsOpen || !IsSuspended) return; SetSuspended(false); NextLine(); }
    public void Close() { IsSuspended = false; completed = null; IsOpen = false; if (panel) panel.SetActive(false); }
    void Update()
    {
        if (!IsOpen || IsSuspended) return;
        visible = Mathf.Min(CurrentPageEnd, visible + (charactersPerSecond <= 0 ? 100000 : Time.unscaledDeltaTime * charactersPerSecond));
        dialogueText.maxVisibleCharacters = Mathf.FloorToInt(visible);
        dialogueText.ForceMeshUpdate();
        bool complete = dialogueText.maxVisibleCharacters >= CurrentPageEnd;
        if (complete && !wasComplete) blinkElapsed = 0;
        else if (complete) blinkElapsed += Time.unscaledDeltaTime;
        wasComplete = complete;
        if (bubbleView) return; // Wait for interaction; keep the final monologue visible.
        if (!continueArrow) return;
        continueArrow.gameObject.SetActive(complete && Mathf.Repeat(blinkElapsed, Mathf.Max(0.05f, arrowBlinkPeriod)) < Mathf.Max(0.05f, arrowBlinkPeriod) * 0.5f);
        if (complete && dialogueText.textInfo.characterCount > 0)
        {
            int lastIndex = CurrentPageEnd - 1;
            while (lastIndex > 0 && !dialogueText.textInfo.characterInfo[lastIndex].isVisible) lastIndex--;
            var last = dialogueText.textInfo.characterInfo[lastIndex];
            var worldEnd = dialogueText.transform.TransformPoint(new Vector3(last.topRight.x, (last.ascender + last.descender) / 2, 0));
            var localEnd = continueArrow.parent.InverseTransformPoint(worldEnd);
            localEnd.x += arrowTextGap + continueArrow.rect.width * continueArrow.pivot.x * Mathf.Abs(continueArrow.localScale.x);
            continueArrow.localPosition = localEnd;
        }
    }
}
