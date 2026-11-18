using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine.Cana
{
    public enum DrinkContents { Empty, Water, Wine }
    public enum BottleTier { Jar, BigJar, SpecialJar }
    public enum MovementEquipment { Barefoot, LeatherShoes, TeleportDevice }

    [System.Serializable]
    public sealed class CanaEquipmentAnimations
    {
        public RuntimeAnimatorController emptyHand;
        public RuntimeAnimatorController jar;
        public RuntimeAnimatorController bigJar;
        public RuntimeAnimatorController specialJar;

        public RuntimeAnimatorController GetController(BottleTier tier, bool carrying)
        {
            if (!carrying) return emptyHand;
            return tier switch
            {
                BottleTier.BigJar => bigJar,
                BottleTier.SpecialJar => specialJar,
                _ => jar
            };
        }
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class CanaPlayer : MonoBehaviour
    {
        [Header("Movement")]
        [Min(0)] public float moveSpeed = 3;
        public Rect movementBounds = new Rect(-9.5f, -5.5625f, 19f, 10.0625f);
        [Header("Interaction")]
        public CanaWaterJar waterJar;
        public CanaJesus jesus;
        [Min(0.05f)] public float fillInterval = 0.4f;
        [Header("Animation")]
        public Animator animator;
        public SpriteRenderer characterSprite;
        public RuntimeAnimatorController emptyHandAnimation;
        public RuntimeAnimatorController carryingAnimation;
        [Min(0)] public float animationSpeed = 1;
        [Header("Equipment appearance")]
        public BottleTier bottleTier = BottleTier.Jar;
        public MovementEquipment movementEquipment = MovementEquipment.Barefoot;
        public CanaEquipmentAnimations barefootAnimations = new CanaEquipmentAnimations();
        public CanaEquipmentAnimations leatherShoesAnimations = new CanaEquipmentAnimations();
        public CanaEquipmentAnimations teleportAnimations = new CanaEquipmentAnimations();
        [Header("Idle animations")]
        public CanaEquipmentAnimations barefootIdleAnimations = new CanaEquipmentAnimations();
        public CanaEquipmentAnimations leatherShoesIdleAnimations = new CanaEquipmentAnimations();
        public CanaEquipmentAnimations teleportIdleAnimations = new CanaEquipmentAnimations();
        [Header("Current drink")]
        [SerializeField] private DrinkContents contents;
        [SerializeField, Range(0, 5)] private int amount;
        public const int Capacity = 5;
        public DrinkContents Contents => contents;
        public int Amount => amount;
        public bool IsPraying { get; private set; }

        private Vector2 position;
        private float nextFillTime;
        private bool isMoving;
        private Rigidbody2D body;
        private readonly System.Collections.Generic.List<RaycastHit2D> movementHits = new System.Collections.Generic.List<RaycastHit2D>();

        private void OnEnable()
        {
            position = transform.position;
            body = GetComponent<Rigidbody2D>();
            isMoving = false;
            RefreshAnimation();
        }

        private void Update()
        {
            isMoving = false;
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                RefreshAnimation();
                return;
            }
            var movement = new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
            if (!IsPraying && movement != Vector2.zero)
            {
                var previousPosition = position;
                position += Vector2.ClampMagnitude(movement, 1) * (moveSpeed * Time.deltaTime);
                position.x = Mathf.Clamp(position.x, movementBounds.xMin, movementBounds.xMax);
                position.y = Mathf.Clamp(position.y, movementBounds.yMin, movementBounds.yMax);
                MoveAxis(Vector2.right, Mathf.Round(position.x * 16) / 16 - body.position.x);
                MoveAxis(Vector2.up, Mathf.Round(position.y * 16) / 16 - body.position.y);
                isMoving = position != previousPosition;
                if (movement.x != 0) characterSprite.flipX = movement.x > 0;
            }
            RefreshAnimation();
            bool pressed = keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
            bool held = keyboard.eKey.isPressed || keyboard.spaceKey.isPressed;
            if (pressed) Interact();
            else if (held && Time.time >= nextFillTime && waterJar && waterJar.IsInRange(this))
                FillWater();
        }

        public void Interact()
        {
            if (IsPraying) return;
            CanaCustomer nearest = null;
            float nearestDistance = 1.25f;
            foreach (var customer in FindObjectsByType<CanaCustomer>())
            {
                if (customer.State != CanaCustomerState.WaitingForOrder) continue;
                float distance = Vector2.Distance(transform.position, customer.transform.position);
                if (distance < nearestDistance) { nearest = customer; nearestDistance = distance; }
            }
            if (nearest) { nearest.TryServe(this); return; }
            if (waterJar && waterJar.IsInRange(this))
                FillWater();
            else if (jesus && jesus.IsInRange(this))
                jesus.TryPray(this);
        }

        private void MoveAxis(Vector2 axis, float displacement)
        {
            if (Mathf.Abs(displacement) < 0.0001f) return;
            var direction = axis * Mathf.Sign(displacement);
            float distance = Mathf.Abs(displacement);
            var filter = new ContactFilter2D();
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            filter.useTriggers = false;
            float skin = Physics2D.defaultContactOffset + 0.01f;
            body.Cast(direction, filter, movementHits, distance + skin);
            float allowedDistance = distance;
            foreach (var hit in movementHits)
                if (Vector2.Dot(hit.normal, direction) < -0.001f)
                    allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0, hit.distance - skin));
            bool blocked = allowedDistance < distance;
            if (blocked) allowedDistance = Mathf.Floor(allowedDistance * 16) / 16;
            body.position += direction * allowedDistance;
            if (blocked)
            {
                if (axis.x != 0) position.x = body.position.x;
                else position.y = body.position.y;
            }
        }

        private void FillWater()
        {
            if (IsPraying || contents == DrinkContents.Wine || amount >= Capacity) return;
            amount++;
            contents = DrinkContents.Water;
            nextFillTime = Time.time + fillInterval;
            RefreshAnimation();
        }

        public bool BeginPrayer()
        {
            if (IsPraying || contents != DrinkContents.Water || amount == 0) return false;
            IsPraying = true;
            isMoving = false;
            RefreshAnimation();
            return true;
        }

        public void FinishPrayer(bool completed)
        {
            if (!IsPraying) return;
            if (completed) contents = DrinkContents.Wine;
            IsPraying = false;
            RefreshAnimation();
        }

        // A future customer consumes one portion only after accepting the order.
        public bool TryServeWine()
        {
            if (IsPraying || contents != DrinkContents.Wine || amount == 0) return false;
            amount--;
            if (amount == 0) contents = DrinkContents.Empty;
            RefreshAnimation();
            return true;
        }

        private void RefreshAnimation()
        {
            if (!animator) return;
            var animations = movementEquipment switch
            {
                MovementEquipment.LeatherShoes => leatherShoesAnimations,
                MovementEquipment.TeleportDevice => teleportAnimations,
                _ => barefootAnimations
            };
            var controller = animations?.GetController(bottleTier, amount > 0);
            if (!isMoving)
            {
                var idleAnimations = movementEquipment switch
                {
                    MovementEquipment.LeatherShoes => leatherShoesIdleAnimations,
                    MovementEquipment.TeleportDevice => teleportIdleAnimations,
                    _ => barefootIdleAnimations
                };
                var idleController = idleAnimations?.GetController(bottleTier, amount > 0);
                if (idleController) controller = idleController;
            }
            if (!controller) controller = amount > 0 ? carryingAnimation : emptyHandAnimation;
            if (animator.runtimeAnimatorController != controller)
                animator.runtimeAnimatorController = controller;
            animator.speed = animationSpeed;
        }

        private void OnDisable()
        {
            if (jesus) jesus.CancelPrayer(this);
        }
    }
}
