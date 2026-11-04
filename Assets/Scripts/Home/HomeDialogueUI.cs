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
    [Min(0)] public float charactersPerSecond = 35;
    [Header("Continue Arrow")]
    [Min(0.05f), Tooltip("한 번 켜졌다 꺼지는 전체 주기(초)")] public float arrowBlinkPeriod = 1f;
    [Min(0), Tooltip("텍스트 끝과 화살표 왼쪽 사이 간격(UI 단위)")] public float arrowTextGap = 24f;
    [Header("Monologue")]
    public Texture2D bubbleTexture;
    [Min(0.1f)] public float monologueLineHold = 2f;
    [System.NonSerialized] public HomeSpeechBubble bubbleView;
    System.Action completed;
    float holdElapsed;
    public bool IsSuspended { get; private set; }
    public bool IsOpen { get; private set; }
    float blinkElapsed;
    bool wasComplete;
    HomeDialogueLine[] lines;
    int index;
    float visible;
    void Awake()
    {
        if (dialogueText && !dialogueText.GetComponent<BitmapTextPixelScale>()) dialogueText.gameObject.AddComponent<BitmapTextPixelScale>();
        if (speakerText && !speakerText.GetComponent<BitmapTextPixelScale>()) speakerText.gameObject.AddComponent<BitmapTextPixelScale>();
        if (panel) panel.SetActive(false);
    }
    void OnValidate() { if (!Application.isPlaying && portraitImage && speakerText && dialogueText) Apply(preview); }
    void Apply(HomeDialogueLine line)
    {
        if (portraitImage) { portraitImage.sprite = line.portrait; portraitImage.enabled = line.portrait != null; }
        if (speakerText) speakerText.text = line.speakerName;
        dialogueText.text = line.content;
    }
    public void Begin(HomeDialogueLine[] dialogue, System.Action onCompleted = null)
    {
        if (dialogue == null || dialogue.Length == 0) return;
        completed = onCompleted;
        lines = dialogue; index = 0; IsOpen = true; panel.SetActive(true); ShowLine();
    }
    void ShowLine() { Apply(lines[index]); holdElapsed = 0; if (bubbleView) bubbleView.Fit(dialogueText); visible = 0; blinkElapsed = 0; wasComplete = false; dialogueText.maxVisibleCharacters = 0; if (continueArrow) continueArrow.gameObject.SetActive(false); }
    public void Advance()
    {
        if (!IsOpen || IsSuspended) return;
        dialogueText.ForceMeshUpdate();
        if (dialogueText.maxVisibleCharacters < dialogueText.textInfo.characterCount)
        { visible = dialogueText.textInfo.characterCount; dialogueText.maxVisibleCharacters = int.MaxValue; return; }
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
        visible += charactersPerSecond <= 0 ? 100000 : Time.unscaledDeltaTime * charactersPerSecond;
        dialogueText.maxVisibleCharacters = Mathf.FloorToInt(visible);
        dialogueText.ForceMeshUpdate();
        bool complete = dialogueText.maxVisibleCharacters >= dialogueText.textInfo.characterCount;
        if (complete && !wasComplete) blinkElapsed = 0;
        else if (complete) blinkElapsed += Time.unscaledDeltaTime;
        wasComplete = complete;
        if (bubbleView)
        {
            if (complete && index < lines.Length - 1)
            {
                holdElapsed += Time.unscaledDeltaTime;
                if (holdElapsed >= monologueLineHold) Advance();
            }
            return; // Keep the final monologue visible; no player input or movement lock.
        }
        if (!continueArrow) return;
        continueArrow.gameObject.SetActive(complete && Mathf.Repeat(blinkElapsed, Mathf.Max(0.05f, arrowBlinkPeriod)) < Mathf.Max(0.05f, arrowBlinkPeriod) * 0.5f);
        if (complete && dialogueText.textInfo.characterCount > 0)
        {
            int lastIndex = dialogueText.textInfo.characterCount - 1;
            while (lastIndex > 0 && !dialogueText.textInfo.characterInfo[lastIndex].isVisible) lastIndex--;
            var last = dialogueText.textInfo.characterInfo[lastIndex];
            var worldEnd = dialogueText.transform.TransformPoint(new Vector3(last.topRight.x, (last.ascender + last.descender) / 2, 0));
            var localEnd = continueArrow.parent.InverseTransformPoint(worldEnd);
            localEnd.x += arrowTextGap + continueArrow.rect.width * continueArrow.pivot.x * Mathf.Abs(continueArrow.localScale.x);
            continueArrow.localPosition = localEnd;
        }
    }
}
