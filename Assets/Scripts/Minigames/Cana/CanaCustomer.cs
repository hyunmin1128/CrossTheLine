using UnityEngine;

namespace CrossTheLine.Cana
{
    public enum CanaCustomerState { NotSpawned, WalkingToChair, WaitingForOrder, Reacting, WalkingToMoneyBin, Leaving, Drinking }
    public enum CanaReaction { None, Heart, Smile, Angry }
    public enum CanaCustomerKind { Normal, Glutton, Impatient, Group, Troublemaker }
    public enum CanaOrderType { Basic, Large, Group }

    public sealed class CanaCustomer : MonoBehaviour
    {
        [Min(0.01f)] public float moveSpeed = 1.5f;
        [Min(0)] public float animationSpeed = 1;
        public SpriteRenderer characterSprite;
        public Animator animator;
        public RuntimeAnimatorController moveAnimation;
        public RuntimeAnimatorController idleAnimation;
        public RuntimeAnimatorController drinkAnimation;
        [Min(0.01f)] public float drinkAnimationSpeed = 1;
        [Min(0)] public float idleAnimationSpeed = 1;
        private CanaReaction servedReaction;
        private bool successRecorded;
        public bool sourceFacesLeft = true;
        public CanaCustomerKind Kind { get; private set; }
        public CanaParty Party { get; private set; }
        public bool PartyLeader { get; private set; }
        public int OrdersCompleted { get; private set; }
        public Sprite[] requestFrames;
        private bool drinkingReported;
        private float nextTrash;
        public bool RoundFinished => owner && owner.round && owner.round.Finished;
        public float EffectiveSecondsPerGauge => Kind == CanaCustomerKind.Impatient && owner && owner.staff && owner.staff.CleanerHired ? secondsPerGauge * 2 : secondsPerGauge;
        public float WaitDuration => gaugeFrames.Length * EffectiveSecondsPerGauge;
        public bool IsAwaitingService => !RoundFinished && state == CanaCustomerState.WaitingForOrder && (!Party || Party.Ordering && !Party.Resolved);
        public int RequiredPortions => Kind == CanaCustomerKind.Glutton ? 2 : 1;
        public SpriteRenderer requestRenderer, gaugeRenderer, reactionRenderer;
        public Sprite[] gaugeFrames;
        public Sprite heart, smile, angry;
        public Vector3 leftRequestOffset, rightRequestOffset, leftGaugeOffset, rightGaugeOffset;
        public Vector3 leftRequestScale = Vector3.one, rightRequestScale = Vector3.one;
        public Vector3 leftGaugeScale = Vector3.one, rightGaugeScale = Vector3.one;
        public Vector3 reactionOffset;
        [Min(0.01f)] public float secondsPerGauge = 1;
        [Min(0)] public float reactionSeconds = 1;
        public int lastGreenFrame = 3;
        [SerializeField] private CanaCustomerState state;
        public CanaCustomerState State => state;
        public CanaOrderType OrderType => Kind == CanaCustomerKind.Group ? CanaOrderType.Group : Kind == CanaCustomerKind.Glutton ? CanaOrderType.Large : CanaOrderType.Basic;
        public CanaReaction Reaction { get; private set; }
        public bool VisitedMoneyBin { get; private set; }
        public bool RejectedAtDoor { get; private set; }
        public int GaugeIndex => Mathf.Clamp(Mathf.FloorToInt(waitTime / EffectiveSecondsPerGauge), 0, gaugeFrames.Length - 1);
        private Vector3[] route;
        private int waypointIndex;
        private Vector3 position;
        private bool seatFacesLeft;
        private float waitTime, waitStarted, reactionTime;
        private CanaCustomerSpawner owner;
        private int seatIndex;

        public void Initialize(CanaCustomerSpawner spawner, int seat, Vector3[] path, bool facesLeft)
        {
            owner = spawner; seatIndex = seat;
            requestRenderer.enabled = gaugeRenderer.enabled = reactionRenderer.enabled = false;
            WalkToChair(path, facesLeft);
        }

        public void RejectAtDoor(CanaCustomerSpawner spawner, CanaCustomerKind kind)
        {
            owner = spawner;
            seatIndex = -1;
            Kind = kind;
            RejectedAtDoor = true;
            reactionRenderer.transform.localPosition = reactionOffset;
            React(CanaReaction.Angry);
        }

        public void WalkToChair(Vector3[] waypoints, bool faceLeftAtSeat)
        {
            seatFacesLeft = faceLeftAtSeat;
            StartRoute(waypoints, CanaCustomerState.WalkingToChair);
        }

        private void StartRoute(Vector3[] path, CanaCustomerState nextState)
        {
            route = path; waypointIndex = 0; position = transform.position;
            state = nextState;
            animator.runtimeAnimatorController = moveAnimation;
        }

        private void Update()
        {
            if (RoundFinished) { animator.speed = 0; return; }
            if (Kind == CanaCustomerKind.Troublemaker && state == CanaCustomerState.WaitingForOrder && Time.time >= nextTrash)
            {
                owner.ScatterTrash(seatIndex);
                nextTrash = Time.time + owner.troubleTrashInterval;
            }
            if (owner && owner.dissatisfaction && owner.dissatisfaction.IsGameOver) { animator.speed = 0; return; }
            if (owner)
            {
                animationSpeed = owner.moveAnimationSpeed;
                idleAnimationSpeed = owner.idleAnimationSpeed;
                drinkAnimationSpeed = owner.drinkAnimationSpeed;
            }
            animator.speed = state == CanaCustomerState.Drinking ? drinkAnimationSpeed :
                state == CanaCustomerState.WaitingForOrder || state == CanaCustomerState.Reacting ? idleAnimationSpeed : animationSpeed;
            if (state == CanaCustomerState.Drinking)
            {
                if (!drinkingReported && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 3)
                {
                    drinkingReported = true;
                    OrdersCompleted++;
                    if (Party) Party.Drank();
                    else if (Kind == CanaCustomerKind.Glutton && OrdersCompleted < 3) BeginOrder();
                    else React(servedReaction);
                }
                return;
            }
            if (state == CanaCustomerState.NotSpawned) return;
            if (state == CanaCustomerState.WaitingForOrder)
            {
                if (Party) return;
                waitTime = Time.time - waitStarted;
                gaugeRenderer.sprite = gaugeFrames[GaugeIndex];
                if (waitTime >= WaitDuration) React(CanaReaction.Angry);
                return;
            }
            if (state == CanaCustomerState.Reacting)
            {
                reactionTime += Time.deltaTime;
                if (reactionTime >= reactionSeconds) Depart();
                return;
            }
            var target = route[waypointIndex];
            float dx = target.x - position.x;
            if (Mathf.Abs(dx) > 0.001f) characterSprite.flipX = (dx > 0) == sourceFacesLeft;
            position = Vector3.MoveTowards(position, target, moveSpeed * Time.deltaTime);
            transform.position = new Vector3(Mathf.Round(position.x * 16) / 16, Mathf.Round(position.y * 16) / 16, position.z);
            if (position != target) return;
            if (++waypointIndex < route.Length) return;
            Arrive();
        }

        private void Arrive()
        {
            if (state == CanaCustomerState.WalkingToChair)
            {
                state = CanaCustomerState.WaitingForOrder;
                characterSprite.flipX = sourceFacesLeft != seatFacesLeft;
                animator.runtimeAnimatorController = idleAnimation;
                requestRenderer.transform.localPosition = seatFacesLeft ? leftRequestOffset : rightRequestOffset;
                requestRenderer.transform.localScale = seatFacesLeft ? leftRequestScale : rightRequestScale;
                gaugeRenderer.transform.localPosition = seatFacesLeft ? leftGaugeOffset : rightGaugeOffset;
                gaugeRenderer.transform.localScale = seatFacesLeft ? leftGaugeScale : rightGaugeScale;
                reactionRenderer.transform.localPosition = reactionOffset;
                reactionRenderer.transform.localScale = seatFacesLeft ? Vector3.one : new Vector3(-1, 1, 1);
                if (!seatFacesLeft) reactionRenderer.transform.localPosition = new Vector3(-reactionOffset.x, reactionOffset.y, 0);
                if (Party) Party.Arrived();
                else BeginOrder();
                return;
            }
            if (state == CanaCustomerState.WalkingToMoneyBin)
            {
                if (!VisitedMoneyBin && owner.wallet)
                {
                    if (Kind == CanaCustomerKind.Glutton) owner.wallet.DepositGold(500);
                    else if (Party) { if (PartyLeader) owner.wallet.DepositGold(700); }
                    else owner.wallet.Deposit(Reaction);
                }
                VisitedMoneyBin = true;
                BeginExit(owner.spawnPoint.position, CanaCustomerState.Leaving);
                return;
            }
            Destroy(gameObject);
        }

        public bool TryServe(CanaPlayer player)
        {
            if (RoundFinished) return false;
            if (state != CanaCustomerState.WaitingForOrder || Vector2.Distance(player.transform.position, transform.position) > 1.25f) return false;
            if (Party) return Party.Serve(player);
            waitTime = Time.time - waitStarted;
            if (waitTime >= WaitDuration) { React(CanaReaction.Angry); return false; }
            if (!player.TryServeWine(Kind == CanaCustomerKind.Glutton ? 2 : 1)) return false;
            CanaAudio.Play(CanaSound.Serve);
            StartDrinking(Kind == CanaCustomerKind.Glutton ? CanaReaction.Heart :
                GaugeIndex <= lastGreenFrame ? CanaReaction.Heart : CanaReaction.Smile);
            return true;
        }

        public bool TryServeFromWorker(Vector3 workerPosition, int portions)
        {
            if (!IsAwaitingService || Vector2.Distance(workerPosition, transform.position) > 1.25f || portions < RequiredPortions) return false;
            if (Party) return Party.ServeFromWorker();
            waitTime = Time.time - waitStarted;
            if (waitTime >= WaitDuration) { React(CanaReaction.Angry); return false; }
            CanaAudio.Play(CanaSound.Serve);
            StartDrinking(Kind == CanaCustomerKind.Glutton ? CanaReaction.Heart :
                GaugeIndex <= lastGreenFrame ? CanaReaction.Heart : CanaReaction.Smile);
            return true;
        }

        public void React(CanaReaction reaction)
        {
            if (!successRecorded && !RejectedAtDoor && owner &&
                (reaction == CanaReaction.Heart || reaction == CanaReaction.Smile))
            {
                successRecorded = true;
                owner.wallet.RecordSuccessfulCustomer();
            }
            animator.runtimeAnimatorController = idleAnimation;
            Reaction = reaction;
            state = CanaCustomerState.Reacting;
            reactionTime = 0;
            requestRenderer.enabled = gaugeRenderer.enabled = false;
            reactionRenderer.sprite = reaction == CanaReaction.Heart ? heart : reaction == CanaReaction.Smile ? smile : angry;
            reactionRenderer.enabled = true;
        }

        private void Depart()
        {
            reactionRenderer.enabled = false;
            if (RejectedAtDoor)
            {
                owner.dissatisfaction.AddComplaint();
                Destroy(gameObject);
                return;
            }
            bool success = Reaction != CanaReaction.Angry;
            if (Party) Party.LeaveSeatsDirty();
            if (!success && owner.dissatisfaction && (!Party || PartyLeader))
                owner.dissatisfaction.AddComplaint(Party ? 2 : 1);
            owner.VacateSeat(seatIndex, this, OrdersCompleted > 0 || Party);
            bool pays = success && (!Party || PartyLeader);
            BeginExit(pays ? owner.moneyPoint.position : owner.spawnPoint.position,
                pays ? CanaCustomerState.WalkingToMoneyBin : CanaCustomerState.Leaving);
        }

        public void MarkSeatDirty()
        {
            if (owner && seatIndex >= 0) owner.MarkDirty(seatIndex);
        }

        public void Configure(CanaCustomerKind kind, CanaParty party, bool leader)
        {
            Kind = kind; Party = party; PartyLeader = leader;
            if (kind == CanaCustomerKind.Glutton) moveSpeed *= 0.5f;
            if (kind == CanaCustomerKind.Impatient) secondsPerGauge *= 0.5f;
            nextTrash = Time.time + owner.troubleTrashInterval;
        }

        private void BeginOrder()
        {
            state = CanaCustomerState.WaitingForOrder;
            waitTime = 0;
            waitStarted = Time.time;
            ShowOrder();
        }

        public void ShowOrder()
        {
            if (Party)
            {
                var scale = requestRenderer.transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                requestRenderer.transform.localScale = scale;
            }
            animator.runtimeAnimatorController = idleAnimation;
            requestRenderer.enabled = gaugeRenderer.enabled = !Party || PartyLeader;
            reactionRenderer.enabled = false;
            gaugeRenderer.sprite = gaugeFrames[0];
            SetRequest(Party ? 4 - Party.Served : 1);
        }

        public void SetRequest(int remaining)
        {
            requestRenderer.sprite = requestFrames[Kind == CanaCustomerKind.Glutton ? 1 :
                Party ? Mathf.Clamp(6 - remaining, 2, 5) : 0];
        }

        public void ShowPatience(float seconds)
        {
            waitTime = seconds;
            gaugeRenderer.sprite = gaugeFrames[GaugeIndex];
        }

        public void StartDrinking(CanaReaction reaction)
        {
            servedReaction = reaction;
            state = CanaCustomerState.Drinking;
            drinkingReported = false;
            requestRenderer.enabled = gaugeRenderer.enabled = false;
            animator.runtimeAnimatorController = drinkAnimation;
            animator.Play(0, 0, 0);
            animator.Update(0);
        }

        private void BeginExit(Vector3 destination, CanaCustomerState next)
        {
            if (!owner.navigation.TryRoute(transform.position, destination, out var path))
            {
                Debug.LogError("No exit route for customer.", this);
                enabled = false;
                return;
            }
            StartRoute(path, next);
        }

        private void OnDestroy()
        {
            if (owner && seatIndex >= 0) owner.ReleaseSeat(seatIndex, this);
        }
    }
}
