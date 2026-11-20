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
        [Header("Q movement skill")]
        [Min(0)] public float skillCooldown = 3;
        [Min(0.01f)] public float dashDuration = 0.14f;
        [Min(0)] public float dashDistance = 2.25f;
        public Transform[] teleportPoints;
        public CanaTeleportEffect teleportEffectPrefab;
        public bool dashAfterimages = true;
        [Min(0.01f)] public float afterimageLifetime = 0.12f;
        private int afterimagesRemaining;
        private float dashTravelled;
        private float nextAfterimageDistance;
        [Range(1, 8)] public int nearbyTeleportChoices = 4;
        public float SkillCooldownRemaining => Mathf.Max(0, nextSkillTime - Time.time);
        public bool IsDashing => dashRemaining > 0;
        private Vector2 lastMovementDirection = Vector2.left;
        private Vector2 dashDirection;
        private float dashRemaining;
        private float nextSkillTime;
        private readonly System.Collections.Generic.List<Collider2D> landingHits = new System.Collections.Generic.List<Collider2D>();

        [Header("Interaction")]
        public CanaWaterJar waterJar;
        public CanaJesus jesus;
        public CanaDissatisfaction dissatisfaction;
        public CanaRound round;
        public CanaCustomerSpawner customerSpawner;
        [Min(0.05f)] public float fillInterval = 0.4f;
        [Header("Animation")]
        public Animator animator;
        public SpriteRenderer characterSprite;
        public RuntimeAnimatorController emptyHandAnimation;
        public RuntimeAnimatorController carryingAnimation;
        [Min(0)] public float animationSpeed = 1;
        [Min(0)] public float idleAnimationSpeed = 1;
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
        [Header("Cleaning")]
        [Min(0.01f)] public float cleaningDuration = 1.8f;
        [Min(0)] public float cleaningAnimationSpeed = 1;
        public RuntimeAnimatorController barefootCleanAnimation;
        public RuntimeAnimatorController shoesCleanAnimation;
        public RuntimeAnimatorController teleportCleanAnimation;
        public bool IsCleaning => cleaningSeat >= 0;
        private int cleaningSeat = -1;
        private float cleaningRemaining;
        private CanaCustomerSpawner cleaningOwner;

        [Header("Current drink")]
        [SerializeField] private DrinkContents contents;
        [SerializeField] private float liquidAmount;
        public int Capacity => bottleTier == BottleTier.BigJar ? 10 : bottleTier == BottleTier.SpecialJar ? int.MaxValue : 5;
        public float LiquidAmount => liquidAmount;
        public bool InfiniteWine => bottleTier == BottleTier.SpecialJar;
        public float MovementSpeedMultiplier => InfiniteWine ? 1 : 1 - 0.5f * Mathf.Clamp01(liquidAmount / Capacity);
        public float CurrentMoveSpeed => moveSpeed * MovementSpeedMultiplier;
        public DrinkContents Contents => InfiniteWine ? DrinkContents.Wine : contents;
        public int Amount => InfiniteWine ? int.MaxValue : Mathf.FloorToInt(liquidAmount + 0.0001f);
        public bool IsPraying { get; private set; }

        private Vector2 position;
        
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
            liquidAmount = Mathf.Clamp(liquidAmount, 0, Capacity);
            isMoving = false;
            if ((round && round.Finished) || (dissatisfaction && dissatisfaction.IsGameOver)) { if (animator) animator.speed = 0; return; }
            if (IsCleaning)
            {
                cleaningRemaining -= Time.deltaTime;
                if (cleaningRemaining <= 0)
                {
                    cleaningOwner.CompleteClean(cleaningSeat, this);
                    cleaningSeat = -1;
                    cleaningOwner = null;
                }
                RefreshAnimation();
                return;
            }
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
            if (movement != Vector2.zero) lastMovementDirection = movement.normalized;
            if (movementEquipment != MovementEquipment.LeatherShoes) dashRemaining = 0;
            bool teleported = keyboard.qKey.wasPressedThisFrame && ActivateMovementSkill(movement)
                && movementEquipment == MovementEquipment.TeleportDevice;
            if (!teleported)
            {
                if (IsDashing)
                {
                    float stepTime = Mathf.Min(Time.deltaTime, dashRemaining);
                    dashRemaining = Mathf.Max(0, dashRemaining - stepTime);
                    Vector2 beforeDashStep = body.position;
                    MoveCharacter(dashDirection * (dashDistance / dashDuration * stepTime));
                    EmitDashAfterimages(beforeDashStep, body.position);
                    if (dashDirection.x != 0) characterSprite.flipX = dashDirection.x > 0;
                }
                else if (movement != Vector2.zero)
                {
                    MoveCharacter(Vector2.ClampMagnitude(movement, 1) * (CurrentMoveSpeed * Time.deltaTime));
                    if (movement.x != 0) characterSprite.flipX = movement.x > 0;
                }
            }
            RefreshAnimation();
            bool pressed = keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
            bool held = keyboard.eKey.isPressed || keyboard.spaceKey.isPressed;
            if (pressed) Interact();
            if (held && waterJar && waterJar.IsInRange(this))
                FillWater(Time.deltaTime);
        }

        private void MoveCharacter(Vector2 displacement)
        {
            var previous = position;
            position += displacement;
            position.x = Mathf.Clamp(position.x, movementBounds.xMin, movementBounds.xMax);
            position.y = Mathf.Clamp(position.y, movementBounds.yMin, movementBounds.yMax);
            MoveAxis(Vector2.right, Mathf.Round(position.x * 16) / 16 - body.position.x);
            MoveAxis(Vector2.up, Mathf.Round(position.y * 16) / 16 - body.position.y);
            isMoving = position != previous;
        }

        public bool ActivateMovementSkill(Vector2 input)
        {
            if (IsCleaning || movementEquipment == MovementEquipment.Barefoot || SkillCooldownRemaining > 0 ||
                (round && round.Finished) || (dissatisfaction && dissatisfaction.IsGameOver)) return false;
            if (movementEquipment == MovementEquipment.LeatherShoes)
            {
                dashDirection = input == Vector2.zero ? lastMovementDirection : input.normalized;
                dashRemaining = dashDuration;
                dashTravelled = 0;
                nextAfterimageDistance = dashDistance / 3;
                afterimagesRemaining = dashAfterimages ? 2 : 0;
                if (dashDirection.x != 0) characterSprite.flipX = dashDirection.x > 0;
                if (dashAfterimages) SpawnAfterimage(body.position);
            }
            else
            {
                if (!TryTeleport(input)) return false;
            }
            nextSkillTime = Time.time + skillCooldown;
            return true;
        }

        private void SpawnAfterimage(Vector2 point)
        {
            var depth = GetComponent<CanaDepthSort>();
            if (depth) depth.Refresh();
            var ghost = new GameObject("Dash Afterimage");
            ghost.transform.position = new Vector3(Mathf.Round(point.x * 16) / 16,
                Mathf.Round(point.y * 16) / 16, transform.position.z);
            ghost.transform.localScale = transform.lossyScale;
            ghost.AddComponent<CanaDashAfterimage>().Initialize(characterSprite, afterimageLifetime);
            if (depth)
            {
                var ghostDepth = ghost.AddComponent<CanaDepthSort>();
                ghostDepth.floorOffset = depth.floorOffset;
                ghostDepth.orderOffset = depth.orderOffset - 1;
                ghostDepth.Refresh();
            }
        }

        private void EmitDashAfterimages(Vector2 from, Vector2 to)
        {
            float distance = Vector2.Distance(from, to);
            if (distance < 0.0001f) return;
            float previousDistance = dashTravelled;
            dashTravelled += distance;
            while (afterimagesRemaining > 0 && dashTravelled >= nextAfterimageDistance)
            {
                float fraction = (nextAfterimageDistance - previousDistance) / distance;
                SpawnAfterimage(Vector2.Lerp(from, to, fraction));
                afterimagesRemaining--;
                nextAfterimageDistance += dashDistance / 3;
            }
        }

        private bool TryTeleport(Vector2 input)
        {
            var choices = new System.Collections.Generic.List<Transform>();
            Vector2 origin = body.position;
            Vector2 direction = input.normalized;
            foreach (var point in teleportPoints)
            {
                if (!point) continue;
                Vector2 delta = (Vector2)point.position - origin;
                if (delta.sqrMagnitude < 0.015625f) continue;
                if (direction != Vector2.zero && Vector2.Dot(delta.normalized, direction) < 0.707f) continue;
                if (CanTeleportTo(point.position)) choices.Add(point);
            }
            if (choices.Count == 0) return false;
            choices.Sort((a, b) => TeleportScore(a.position, origin, direction).CompareTo(TeleportScore(b.position, origin, direction)));
            int index = direction == Vector2.zero ? Random.Range(0, Mathf.Min(nearbyTeleportChoices, choices.Count)) : 0;
            Vector2 target = choices[index].position;
            target = new Vector2(Mathf.Round(target.x * 16) / 16, Mathf.Round(target.y * 16) / 16);
            Vector2 deltaPosition = target - origin;
            SpawnTeleportEffect(origin);
            body.position = target;
            transform.position = new Vector3(target.x, target.y, transform.position.z);
            position = target;
            Physics2D.SyncTransforms();
            if (deltaPosition.x != 0) characterSprite.flipX = deltaPosition.x > 0;
            return true;
        }

        private void SpawnTeleportEffect(Vector2 origin)
        {
            if (!teleportEffectPrefab) return;
            Vector2 foot = Vector2.zero;
            var anchors = jesus && jesus.prayerEffect ? jesus.prayerEffect.GetComponent<CanaOneShotEffect>() : null;
            if (anchors)
                foreach (var anchor in anchors.footAnchors)
                    if (anchor.sprite == characterSprite.sprite) { foot = anchor.position; break; }
            if (characterSprite.flipX) foot.x = -foot.x;
            if (characterSprite.flipY) foot.y = -foot.y;
            Vector3 point = (Vector3)origin + transform.TransformVector(foot);
            point.x = Mathf.Round(point.x * 16) / 16;
            point.y = Mathf.Round(point.y * 16) / 16;
            var depth = GetComponent<CanaDepthSort>();
            if (depth) depth.Refresh();
            var effect = Instantiate(teleportEffectPrefab, point, Quaternion.identity);
            var renderer = effect.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = characterSprite.sortingLayerID;
            renderer.sortingOrder = characterSprite.sortingOrder + 1;
        }

        private static float TeleportScore(Vector2 target, Vector2 origin, Vector2 direction)
        {
            Vector2 delta = target - origin;
            if (direction == Vector2.zero) return delta.sqrMagnitude;
            float dot = Vector2.Dot(delta.normalized, direction);
            return delta.sqrMagnitude / (dot * dot * dot * dot);
        }

        public bool CanTeleportTo(Vector2 target)
        {
            target = new Vector2(Mathf.Round(target.x * 16) / 16, Mathf.Round(target.y * 16) / 16);
            if (!movementBounds.Contains(target)) return false;
            if (!body) body = GetComponent<Rigidbody2D>();
            var collider = GetComponent<Collider2D>();
            var bounds = collider.bounds;
            Vector2 center = target + (Vector2)bounds.center - body.position;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            filter.useTriggers = false;
            Physics2D.OverlapBox(center, bounds.size, 0, filter, landingHits);
            foreach (var hit in landingHits)
                if (hit.attachedRigidbody != body) return false;
            return true;
        }

        public void Interact()
        {
            if (IsCleaning || IsPraying || ((round && round.Finished) || (dissatisfaction && dissatisfaction.IsGameOver))) return;
            CanaCustomer nearest = null;
            float nearestDistance = 1.25f;
            foreach (var customer in FindObjectsByType<CanaCustomer>())
            {
                if (customer.State != CanaCustomerState.WaitingForOrder) continue;
                float distance = Vector2.Distance(transform.position, customer.transform.position);
                if (distance < nearestDistance) { nearest = customer; nearestDistance = distance; }
            }
            if (customerSpawner && customerSpawner.TryClean(this)) return;
            if (nearest) { nearest.TryServe(this); return; }
            if (waterJar && waterJar.IsInRange(this)) return;
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

        public void FillWater(float seconds)
        {
            if (IsCleaning || IsPraying || InfiniteWine || seconds <= 0 || liquidAmount >= Capacity) return;
            liquidAmount = Mathf.Min(Capacity, liquidAmount + seconds / fillInterval);
            contents = DrinkContents.Water;
            RefreshAnimation();
        }

        public bool BeginPrayer()
        {
            if (IsCleaning || IsPraying || InfiniteWine || contents != DrinkContents.Water || liquidAmount <= 0) return false;
            contents = DrinkContents.Wine;
            IsPraying = true;
            RefreshAnimation();
            return true;
        }

        public void FinishPrayer(bool completed)
        {
            if (!IsPraying) return;
            IsPraying = false;
            RefreshAnimation();
        }

        // A future customer consumes one portion only after accepting the order.
        public bool TryServeWine(int portions = 1)
        {
            if (IsCleaning || portions <= 0) return false;
            if (InfiniteWine) return true;
            if (IsPraying || contents != DrinkContents.Wine || Amount < portions) return false;
            liquidAmount = Mathf.Max(0, liquidAmount - portions);
            if (liquidAmount < 0.0001f) { liquidAmount = 0; contents = DrinkContents.Empty; }
            RefreshAnimation();
            return true;
        }

        public bool BeginCleaning(CanaCustomerSpawner owner, int seat, bool facesLeft)
        {
            if (IsCleaning || IsPraying || (round && round.Finished)) return false;
            cleaningOwner = owner;
            cleaningSeat = seat;
            cleaningRemaining = cleaningDuration;
            dashRemaining = 0;
            isMoving = false;
            characterSprite.flipX = !facesLeft;
            RefreshAnimation();
            return true;
        }

        private void RefreshAnimation()
        {
            if (!animator) return;
            if (IsCleaning)
            {
                var cleanController = movementEquipment switch
                {
                    MovementEquipment.LeatherShoes => shoesCleanAnimation,
                    MovementEquipment.TeleportDevice => teleportCleanAnimation,
                    _ => barefootCleanAnimation
                };
                if (animator.runtimeAnimatorController != cleanController)
                    animator.runtimeAnimatorController = cleanController;
                animator.speed = cleaningAnimationSpeed;
                return;
            }
            var animations = movementEquipment switch
            {
                MovementEquipment.LeatherShoes => leatherShoesAnimations,
                MovementEquipment.TeleportDevice => teleportAnimations,
                _ => barefootAnimations
            };
            var controller = animations?.GetController(bottleTier, (InfiniteWine || liquidAmount > 0));
            if (!isMoving)
            {
                var idleAnimations = movementEquipment switch
                {
                    MovementEquipment.LeatherShoes => leatherShoesIdleAnimations,
                    MovementEquipment.TeleportDevice => teleportIdleAnimations,
                    _ => barefootIdleAnimations
                };
                var idleController = idleAnimations?.GetController(bottleTier, (InfiniteWine || liquidAmount > 0));
                if (idleController) controller = idleController;
            }
            if (!controller) controller = (InfiniteWine || liquidAmount > 0) ? carryingAnimation : emptyHandAnimation;
            if (animator.runtimeAnimatorController != controller)
                animator.runtimeAnimatorController = controller;
            animator.speed = isMoving ? animationSpeed : idleAnimationSpeed;
        }

        private void OnDestroy()
        {
            if (cleaningOwner && IsCleaning) cleaningOwner.AbandonCleaning(cleaningSeat, this);
        }

        private void OnDisable()
        {
            dashRemaining = 0;
            if (jesus) jesus.CancelPrayer(this);
        }
    }
}
