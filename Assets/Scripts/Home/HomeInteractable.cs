using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class HomeInteractable : MonoBehaviour
{
    public enum ActionType { Dialogue, LoadScene }
    public ActionType action;
    [TextArea(2, 4)]
    public string prompt = "[E]\nTalk";
    public string sceneName = "MainScene1";
    public HomeDialogueLine[] lines;
    public HomeDialogueLine[] afterDialogue = { new HomeDialogueLine { content = "See you later." } };
    public Vector3 bubbleOffset = new Vector3(0, 0.5f, 0);
    public bool DialogueCompleted { get; private set; }
    public bool CanInteract => isActiveAndEnabled && !loading && !DialogueCompleted;
    HomeDialogueUI activeDialogue;
    HomeDialogueUI monologue;
    public Transform floatingIcon;
    public TMP_Text promptText;
    public float bobHeight = 0.12f;
    public float bobSpeed = 2.5f;
    Vector3 iconOrigin;
    bool loading;
    void Awake() { if (floatingIcon) iconOrigin = floatingIcon.localPosition; }
    void Update()
    {
        if (floatingIcon) floatingIcon.position = (floatingIcon.parent ? floatingIcon.parent.TransformPoint(iconOrigin) : iconOrigin) + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        if (promptText) promptText.text = prompt;
    }
    public void SetNearby(bool nearby, bool dialogueOpen)
    {
        if (floatingIcon) floatingIcon.gameObject.SetActive(CanInteract && !nearby && !dialogueOpen);
        if (promptText) promptText.gameObject.SetActive(CanInteract && nearby && !dialogueOpen);
    }
    public void Interact(HomeDialogueUI ui)
    {
        if (!CanInteract || (ui && ui.IsOpen)) return;
        if (action == ActionType.Dialogue)
        {
            if (ui && lines != null && lines.Length > 0)
            {
                activeDialogue = ui;
                ui.Begin(lines, FinishDialogue);
            }
        }
        else if (!loading && Application.CanStreamedLevelBeLoaded(sceneName)) { loading = true; SceneManager.LoadSceneAsync(sceneName); }
    }
    void FinishDialogue()
    {
        DialogueCompleted = true;
        SetNearby(false, false);
        if (activeDialogue && afterDialogue != null && afterDialogue.Length > 0)
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            Vector3 anchor = sprite ? new Vector3(sprite.bounds.center.x, sprite.bounds.max.y, transform.position.z) : transform.position;
            monologue = HomeSpeechBubble.Create(activeDialogue, transform, anchor + bubbleOffset);
            monologue.Begin(afterDialogue);
        }
        activeDialogue = null;
    }
    void OnDisable()
    {
        if (activeDialogue) activeDialogue.Close();
        activeDialogue = null;
    }
}
