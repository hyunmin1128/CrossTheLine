using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class JesusController : MonoBehaviour
{
    [Min(0)] public float moveSpeed = 4;
    [Range(0, 80)] public float maxSlopeAngle = 50;
    public LayerMask groundLayers = ~0;
    public Key interactionKey = Key.E;
    public Animator animator;
    public SpriteRenderer characterSprite;
    public HomeDialogueUI dialogueUI;
    Rigidbody2D body;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    readonly HashSet<HomeInteractable> nearby = new HashSet<HomeInteractable>();
    HomeInteractable[] interactables;
    float input;
    public bool MovementLocked { get; private set; }
    public float ScriptedMove { get; set; }
    public void SetMovementLocked(bool locked) { MovementLocked = locked; input = 0; if (body) body.linearVelocity = Vector2.zero; }
    [Min(0)] public float groundSnapDistance = 0.3f;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        interactables = FindObjectsByType<HomeInteractable>(FindObjectsSortMode.None);
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        bool talking = dialogueUI && dialogueUI.IsOpen;
        input = 0;
        if (keyboard != null && !talking && !MovementLocked)
            input = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0) - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);
        if (MovementLocked) input = ScriptedMove;
        if (animator) animator.SetBool("IsWalking", input != 0);
        if (characterSprite && input != 0) characterSprite.flipX = input < 0;
        HomeInteractable target = null;
        float closest = float.PositiveInfinity;
        foreach (var item in nearby)
        {
            if (!item || !item.CanInteract) continue;
            float distance = (item.transform.position - transform.position).sqrMagnitude;
            if (distance < closest) { closest = distance; target = item; }
        }
        if (!MovementLocked && keyboard != null && keyboard[interactionKey].wasPressedThisFrame)
        {
            if (talking) dialogueUI.Advance(); else if (target) target.Interact(dialogueUI);
        }
        talking = dialogueUI && dialogueUI.IsOpen;
        foreach (var item in interactables) if (item) item.SetNearby(nearby.Contains(item), talking);
    }
    void FixedUpdate()
    {
        var filter = new ContactFilter2D(); filter.SetLayerMask(groundLayers); filter.useTriggers = false;
        int count = body.Cast(Vector2.down, filter, hits, groundSnapDistance);
        Vector2 normal = Vector2.up;
        bool grounded = false; float groundDistance = 0;
        for (int i = 0; i < count; i++)
            if (hits[i].distance > 0.001f && Vector2.Angle(hits[i].normal, Vector2.up) <= maxSlopeAngle) { normal = hits[i].normal; groundDistance = hits[i].distance; grounded = true; break; }
        if (grounded)
        {
            var tangent = new Vector2(normal.y, -normal.x);
            body.linearVelocity = tangent * (input * moveSpeed);
            if (input != 0) body.position += Vector2.down * Mathf.Max(0, groundDistance - 0.02f);
            body.gravityScale = input == 0 ? 0 : 1;
        }
        else { body.gravityScale = 1; body.linearVelocity = new Vector2(input * moveSpeed, body.linearVelocity.y); }
    }
    void OnTriggerEnter2D(Collider2D other) { var item = other.GetComponentInParent<HomeInteractable>(); if (item) nearby.Add(item); }
    void OnTriggerExit2D(Collider2D other) { var item = other.GetComponentInParent<HomeInteractable>(); if (item) nearby.Remove(item); }
    void OnDisable() { nearby.Clear(); }
}
