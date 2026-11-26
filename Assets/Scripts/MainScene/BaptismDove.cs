using System.Collections;
using UnityEngine;

public sealed class BaptismDove : MonoBehaviour
{
    public enum FlightPhase { Hidden, Arriving, Hovering, Departing }
    public SpriteRenderer image;
    public Animator animator;
    [Min(.05f)] public float arrivalSeconds = .8f;
    [Min(0)] public float hoverSeconds = 1.2f;
    [Min(.05f)] public float departureSeconds = .8f;
    [Min(0)] public float heightAboveHead = .75f;
    [Min(0)] public float hoverBobHeight = .0625f;
    [Min(.05f)] public float hoverBobPeriod = .6f;
    [Min(0)] public float animationSpeed = 1;
    public int pixelsPerUnit = 16;
    public FlightPhase Phase { get; private set; }
    Vector3 Above(SpriteRenderer target) => new Vector3(target.bounds.center.x, target.bounds.max.y + heightAboveHead, 0);
    void Position(Vector3 p)
    {
        float grid = Mathf.Max(1, pixelsPerUnit);
        p.x = Mathf.Round(p.x * grid) / grid;
        p.y = Mathf.Round(p.y * grid) / grid;
        transform.position = p;
    }
    void SetPhase(FlightPhase phase)
    {
        Phase = phase;
        Debug.Log("[Baptism Dove] " + phase);
    }
    public IEnumerator FlyAbove(SpriteRenderer target, Camera view)
    {
        gameObject.SetActive(true);
        image.enabled = true;
        animator.enabled = true;
        animator.speed = animationSpeed;
        animator.Play(0, 0, 0);
        Vector3 hover = Above(target);
        float top = view ? view.ViewportToWorldPoint(new Vector3(.5f, 1, Mathf.Abs(view.transform.position.z))).y : hover.y + 4;
        float offscreen = Mathf.Max(top, hover.y) + 1.5f;
        Vector3 start = new Vector3(hover.x + 3, offscreen, 0);
        Position(start);
        SetPhase(FlightPhase.Arriving);
        for (float t = 0; t < arrivalSeconds; t += Time.deltaTime)
        {
            hover = Above(target);
            float u = Mathf.SmoothStep(0, 1, t / arrivalSeconds);
            Position(Vector3.Lerp(start, hover, u));
            yield return null;
        }
        SetPhase(FlightPhase.Hovering);
        for (float t = 0; t < hoverSeconds; t += Time.deltaTime)
        {
            Position(Above(target) + Vector3.up * (Mathf.Sin(t * Mathf.PI * 2 / hoverBobPeriod) * hoverBobHeight));
            yield return null;
        }
        start = transform.position;
        Vector3 end = new Vector3(start.x - 3, offscreen, 0);
        SetPhase(FlightPhase.Departing);
        for (float t = 0; t < departureSeconds; t += Time.deltaTime)
        {
            float u = Mathf.Clamp01(t / departureSeconds);
            Position(Vector3.Lerp(start, end, u * u));
            yield return null;
        }
        Position(end);
        Hide();
    }
    public void Hide()
    {
        if (image) image.enabled = false;
        if (animator) animator.enabled = false;
        if (Phase != FlightPhase.Hidden) SetPhase(FlightPhase.Hidden);
        gameObject.SetActive(false);
    }
}
