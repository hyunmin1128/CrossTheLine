using System.Collections;
using UnityEngine;

public class DesertJourneySequence : MonoBehaviour
{
    public JesusController player;
    public HomeInteractable yohan;
    public Transform tiredStart;
    public Transform collapsePoint;
    public AnimationClip walkClip;
    public AnimationClip tiredClip;
    public Sprite collapsedSprite;
    [Min(0.1f)] public float finalSpeed = 1.1f;
    [Min(0)] public float collapseHold = 1.5f;
    [Min(0.1f)] public float fadeDuration = 1.2f;
    public string nextScene = "MiniGame1";
    public bool IsPlaying { get; private set; }
    public bool IsTired { get; private set; }
    public bool HasCollapsed { get; private set; }
    AnimatorOverrideController tiredController;
    float footY;

    void Update()
    {
        // Completion includes the conversation after the baptism animation.
        if (!IsPlaying && yohan && yohan.DialogueCompleted) StartCoroutine(Perform());
    }

    IEnumerator Perform()
    {
        IsPlaying = true;
        float initialSpeed = player.moveSpeed;
        footY = player.characterSprite.transform.localPosition.y + player.characterSprite.sprite.bounds.min.y * player.characterSprite.transform.localScale.y;
        player.SetMovementLocked(true);
        player.ScriptedMove = 1;
        Debug.Log("[DesertJourney] Automatic walk started.");
        while (player.transform.position.x < collapsePoint.position.x)
        {
            float progress = Mathf.InverseLerp(tiredStart.position.x, collapsePoint.position.x, player.transform.position.x);
            if (!IsTired && player.transform.position.x >= tiredStart.position.x)
            {
                IsTired = true;
                tiredController = new AnimatorOverrideController(player.animator.runtimeAnimatorController);
                tiredController[walkClip] = tiredClip;
                player.animator.runtimeAnimatorController = tiredController;
                Debug.Log("[DesertJourney] Tired walk started.");
            }
            player.moveSpeed = Mathf.Lerp(initialSpeed, finalSpeed, Mathf.SmoothStep(0, 1, progress));
            if (IsTired) player.animator.speed = Mathf.Lerp(0.8f, 0.45f, progress);
            yield return null;
        }
        player.ScriptedMove = 0;
        player.SetMovementLocked(true);
        player.animator.enabled = false;
        player.characterSprite.sprite = collapsedSprite;
        HasCollapsed = true;
        Debug.Log("[DesertJourney] Collapsed.");
        yield return new WaitForSeconds(collapseHold);
        SceneFadeTransition.Load(nextScene, fadeDuration);
    }

    void LateUpdate()
    {
        if (!IsTired && !HasCollapsed) return;
        var visual = player.characterSprite;
        var position = visual.transform.localPosition;
        position.y = footY - visual.sprite.bounds.min.y * visual.transform.localScale.y;
        visual.transform.localPosition = position;
    }

    void OnDestroy() { if (tiredController) Destroy(tiredController); }
}
