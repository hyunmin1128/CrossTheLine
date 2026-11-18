using UnityEngine;

namespace CrossTheLine.Cana
{
    public enum CanaCustomerState { NotSpawned, WalkingToChair, WaitingForOrder, Reacting, WalkingToMoneyBin, Leaving }
    public enum CanaReaction { None, Heart, Smile, Angry }
    public enum CanaOrderType { Basic, Large, Group }

    public sealed class CanaCustomer : MonoBehaviour
    {
        [Min(0.01f)] public float moveSpeed = 1.5f;
        [Min(0)] public float animationSpeed = 1;
        public SpriteRenderer characterSprite;
        public Animator animator;
        public RuntimeAnimatorController moveAnimation;
        public RuntimeAnimatorController idleAnimation;
        public bool sourceFacesLeft = true;
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
        public CanaOrderType OrderType => CanaOrderType.Basic;
        public CanaReaction Reaction { get; private set; }
        public bool VisitedMoneyBin { get; private set; }
        public int GaugeIndex => Mathf.Clamp(Mathf.FloorToInt(waitTime / secondsPerGauge), 0, gaugeFrames.Length - 1);
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
            animator.speed = animationSpeed;
            if (state == CanaCustomerState.NotSpawned) return;
            if (state == CanaCustomerState.WaitingForOrder)
            {
                waitTime = Time.time - waitStarted;
                gaugeRenderer.sprite = gaugeFrames[GaugeIndex];
                if (waitTime >= gaugeFrames.Length * secondsPerGauge) React(CanaReaction.Angry);
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
                waitTime = 0;
                waitStarted = Time.time;
                gaugeRenderer.sprite = gaugeFrames[0];
                requestRenderer.enabled = gaugeRenderer.enabled = true;
                return;
            }
            if (state == CanaCustomerState.WalkingToMoneyBin)
            {
                VisitedMoneyBin = true;
                BeginExit(owner.spawnPoint.position, CanaCustomerState.Leaving);
                return;
            }
            Destroy(gameObject);
        }

        public bool TryServe(CanaPlayer player)
        {
            if (state != CanaCustomerState.WaitingForOrder || Vector2.Distance(player.transform.position, transform.position) > 1.25f) return false;
            waitTime = Time.time - waitStarted;
            if (waitTime >= gaugeFrames.Length * secondsPerGauge) { React(CanaReaction.Angry); return false; }
            if (!player.TryServeWine()) return false;
            React(GaugeIndex <= lastGreenFrame ? CanaReaction.Heart : CanaReaction.Smile);
            return true;
        }

        private void React(CanaReaction reaction)
        {
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
            bool success = Reaction != CanaReaction.Angry;
            BeginExit(success ? owner.moneyPoint.position : owner.spawnPoint.position,
                success ? CanaCustomerState.WalkingToMoneyBin : CanaCustomerState.Leaving);
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
            if (owner) owner.ReleaseSeat(seatIndex, this);
        }
    }
}
