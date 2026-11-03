using System.Collections;
using UnityEngine;

public class BaptismSequence : MonoBehaviour
{
    public JesusController player;
    public HomeDialogueUI dialogue;
    public Animator yohanAnimator;
    public Transform baptismSpot;
    public Sprite baptismSprite;
    public ParticleSystem holyLight;
    [Min(0.5f)] public float duration = 3f;
    public bool IsPlaying { get; private set; }
    Sprite originalSprite;
    Vector3 originalSpritePosition;
    bool originalAnimatorEnabled;
    bool originalFlip;

    public void Play()
    {
        if (!IsPlaying) StartCoroutine(Perform());
    }
    IEnumerator Perform()
    {
        IsPlaying = true;
        dialogue.SetSuspended(true);
        player.SetMovementLocked(true);
        originalSprite = player.characterSprite.sprite;
        originalSpritePosition = player.characterSprite.transform.localPosition;
        originalAnimatorEnabled = player.animator.enabled;
        originalFlip = player.characterSprite.flipX;
        // Approach the ceremony position along the same ground-following path.
        while (Mathf.Abs(player.transform.position.x - baptismSpot.position.x) > 0.08f)
        {
            player.ScriptedMove = Mathf.Clamp((baptismSpot.position.x - player.transform.position.x) / Mathf.Max(0.01f, player.moveSpeed * Time.fixedDeltaTime), -1, 1);
            yield return null;
        }
        player.ScriptedMove = 0;
        player.animator.enabled = false;
        player.characterSprite.sprite = baptismSprite;
        player.characterSprite.flipX = false;
        // Align different sprite pivots at the feet rather than shifting the collider.
        float foot = originalSpritePosition.y + originalSprite.bounds.min.y;
        var pos = originalSpritePosition; pos.y = foot - baptismSprite.bounds.min.y;
        player.characterSprite.transform.localPosition = pos;
        yohanAnimator.Play("YohanBaptism", 0, 0);
        if (holyLight) holyLight.Play();
        yield return new WaitForSeconds(duration);
        Restore();
        dialogue.ResumeAfterEvent();
    }
    void Restore()
    {
        if (!IsPlaying) return;
        player.ScriptedMove = 0;
        player.characterSprite.sprite = originalSprite ? originalSprite : player.characterSprite.sprite;
        player.characterSprite.transform.localPosition = originalSpritePosition;
        player.characterSprite.flipX = originalFlip;
        player.animator.enabled = originalAnimatorEnabled;
        player.SetMovementLocked(false);
        if (yohanAnimator) yohanAnimator.Play("YohanIdle", 0, 0);
        if (holyLight) holyLight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        IsPlaying = false;
    }
    void OnDisable() { StopAllCoroutines(); Restore(); if (dialogue && dialogue.IsSuspended) dialogue.Close(); }
}
