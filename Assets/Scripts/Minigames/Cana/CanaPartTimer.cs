using UnityEngine;

namespace CrossTheLine.Cana
{
    public enum CanaStaffState { Idle, Returning, ToWine, TakingWine, ToServe, Serving, ToClean, ChangeToClean, Cleaning, ChangeBack }

    public sealed class CanaPartTimer : MonoBehaviour
    {
        public CanaStaffManager manager;
        public CanaStaffKind kind;
        public Transform home;
        public SpriteRenderer image;
        public Sprite[] idle, move, serve, clean, change;
        public float moveSpeed = 2.5f;
        public float framesPerSecond = 10;
        [Tooltip("Idle timing relative to the customer Idle Animation Speed; movement is unchanged.")]
        [Min(0)] public float idleSpeedMultiplier = 1;
        public bool overrideIdleFacing;
        public bool idleFlipX;
        public float pickupSeconds = .4f;
        public float servingSeconds = .25f;
        public float cleaningSeconds = 1.8f;
        public float changeSeconds = .7f;
        public float searchInterval = .2f;
        public CanaStaffState State { get; private set; }
        public CanaCustomer Target { get; private set; }
        public int CleanSeat { get; private set; } = -1;
        public int CarriedPortions { get; private set; }
        private Vector3[] route;
        private int waypoint;
        private Vector3 position;
        private float elapsed, nextSearch;
        private float animationElapsed;
        private int reverseFrom;
        public float CurrentAnimationSpeed
        {
            get
            {
                if (kind == CanaStaffKind.Robot) return 1;
                return State switch
                {
                    CanaStaffState.Idle => manager.spawner.idleAnimationSpeed * idleSpeedMultiplier,
                    CanaStaffState.TakingWine => manager.spawner.idleAnimationSpeed,
                    CanaStaffState.Returning or CanaStaffState.ToWine or CanaStaffState.ToServe or CanaStaffState.ToClean => manager.spawner.moveAnimationSpeed,
                    _ => 1
                };
            }
        }

        private void OnEnable()
        {
            position = transform.position;
            SetState(CanaStaffState.Idle);
        }

        public bool Assign(CanaCustomer customer, int seat, Vector3[] approach)
        {
            Target = customer;
            CleanSeat = seat;
            CarriedPortions = 0;
            if (customer && kind == CanaStaffKind.Server)
            {
                if (!manager.spawner.navigation.TryRoute(transform.position, manager.winePickup.position, out var toWine)) return false;
                Travel(toWine, CanaStaffState.ToWine);
            }
            else
            {
                if (customer) CarriedPortions = customer.RequiredPortions;
                Travel(approach, customer ? CanaStaffState.ToServe : CanaStaffState.ToClean);
            }
            return true;
        }

        private bool JobValid()
        {
            if (Target) return Target.IsAwaitingService;
            return CleanSeat >= 0 && manager.spawner.CanStaffClean(CleanSeat, this);
        }

        private void Update()
        {
            if (manager.spawner.round.Finished || manager.spawner.dissatisfaction.IsGameOver || Time.deltaTime <= 0) return;
            elapsed += Time.deltaTime;
            animationElapsed += Time.deltaTime * CurrentAnimationSpeed;
            if (State != CanaStaffState.Idle && State != CanaStaffState.Returning && State != CanaStaffState.ChangeBack && !JobValid())
                CancelJob();
            if ((State == CanaStaffState.Idle || State == CanaStaffState.Returning) && Time.time >= nextSearch)
            {
                nextSearch = Time.time + searchInterval;
                manager.TryAssign(this);
            }
            switch (State)
            {
                case CanaStaffState.Returning:
                case CanaStaffState.ToWine:
                case CanaStaffState.ToServe:
                case CanaStaffState.ToClean:
                    Walk();
                    break;
                case CanaStaffState.TakingWine:
                    if (elapsed >= pickupSeconds)
                    {
                        CarriedPortions = Target.RequiredPortions;
                        if (manager.TryApproach(transform.position, Target.transform.position, out var toGuest)) Travel(toGuest, CanaStaffState.ToServe);
                        else CancelJob();
                    }
                    break;
                case CanaStaffState.Serving:
                    if (elapsed >= servingSeconds)
                    {
                        Target.TryServeFromWorker(transform.position, CarriedPortions);
                        ReturnHome();
                    }
                    break;
                case CanaStaffState.ChangeToClean:
                    if (elapsed >= changeSeconds) SetState(CanaStaffState.Cleaning);
                    break;
                case CanaStaffState.Cleaning:
                    if (elapsed >= cleaningSeconds)
                    {
                        manager.spawner.CompleteStaffClean(CleanSeat, this);
                        if (kind == CanaStaffKind.Robot) Reverse();
                        else ReturnHome();
                    }
                    break;
                case CanaStaffState.ChangeBack:
                    if (elapsed >= changeSeconds * (reverseFrom + 1) / change.Length) ReturnHome();
                    break;
            }
            Draw();
        }

        private void Travel(Vector3[] path, CanaStaffState state)
        {
            route = path;
            waypoint = 0;
            position = transform.position;
            SetState(state);
        }

        private void Walk()
        {
            float budget = moveSpeed * Time.deltaTime;
            while (waypoint < route.Length && budget > 0)
            {
                var target = route[waypoint];
                var delta = target - position;
                if (Mathf.Abs(delta.x) > .001f) image.flipX = (delta.x > 0) ^ (image.transform.lossyScale.x < 0);
                float step = Mathf.Min(budget, delta.magnitude);
                position = Vector3.MoveTowards(position, target, step);
                budget -= step;
                if ((position - target).sqrMagnitude < .000001f) waypoint++;
                else break;
            }
            transform.position = new Vector3(Mathf.Round(position.x * 16) / 16, Mathf.Round(position.y * 16) / 16, position.z);
            if (waypoint < route.Length) return;
            switch (State)
            {
                case CanaStaffState.Returning:
                    transform.position = home.position;
                    position = home.position;
                    SetState(CanaStaffState.Idle);
                    break;
                case CanaStaffState.ToWine: SetState(CanaStaffState.TakingWine); break;
                case CanaStaffState.ToServe: SetState(CanaStaffState.Serving); break;
                case CanaStaffState.ToClean:
                    if (!manager.spawner.TryBeginStaffClean(CleanSeat, this)) { CancelJob(); break; }
                    var delta = manager.spawner.seats[CleanSeat].point.position.x - transform.position.x;
                    if (Mathf.Abs(delta) > .001f) image.flipX = (delta > 0) ^ (image.transform.lossyScale.x < 0);
                    SetState(kind == CanaStaffKind.Robot ? CanaStaffState.ChangeToClean : CanaStaffState.Cleaning);
                    break;
            }
        }

        private void CancelJob()
        {
            if (kind == CanaStaffKind.Robot && (State == CanaStaffState.ChangeToClean || State == CanaStaffState.Cleaning)) Reverse();
            else ReturnHome();
        }

        private void Reverse()
        {
            reverseFrom = State == CanaStaffState.ChangeToClean ? Mathf.Clamp(Mathf.FloorToInt(elapsed / changeSeconds * change.Length),0,change.Length-1) : change.Length - 1;
            ReleaseJob();
            SetState(CanaStaffState.ChangeBack);
        }

        private void ReleaseJob()
        {
            if (CleanSeat >= 0) manager.spawner.CancelStaffClean(CleanSeat, this);
            manager.Release(this);
            Target = null;
            CleanSeat = -1;
            CarriedPortions = 0;
        }

        private void ReturnHome()
        {
            ReleaseJob();
            if (manager.spawner.navigation.TryRoute(transform.position, home.position, out var path)) Travel(path, CanaStaffState.Returning);
            else SetState(CanaStaffState.Idle);
            nextSearch = Time.time;
        }

        private void SetState(CanaStaffState state)
        {
            if (State == CanaStaffState.Cleaning && state != CanaStaffState.Cleaning) CanaAudio.EndMop(this);
            if (State == CanaStaffState.ChangeToClean || State == CanaStaffState.ChangeBack) CanaAudio.StopRobotTransform(this);
            State = state;
            if (kind == CanaStaffKind.Robot && state == CanaStaffState.ChangeToClean) CanaAudio.RobotTransform(this, changeSeconds);
            if (kind == CanaStaffKind.Robot && state == CanaStaffState.ChangeBack)
                CanaAudio.RobotTransform(this, changeSeconds * (reverseFrom + 1) / Mathf.Max(1, change.Length));
            if (state == CanaStaffState.Cleaning) CanaAudio.BeginMop(this);
            elapsed = 0;
            animationElapsed = 0;
            Draw();
        }

        private void Draw()
        {
            if (State == CanaStaffState.Idle && overrideIdleFacing) image.flipX = idleFlipX;
            Sprite[] frames = State switch
            {
                CanaStaffState.Cleaning => clean,
                CanaStaffState.ChangeToClean or CanaStaffState.ChangeBack => change,
                CanaStaffState.ToServe or CanaStaffState.Serving => serve,
                CanaStaffState.ToWine or CanaStaffState.Returning or CanaStaffState.ToClean => move,
                _ => idle
            };
            if (frames == null || frames.Length == 0) return;
            int index = Mathf.FloorToInt(animationElapsed * framesPerSecond) % frames.Length;
            if (State == CanaStaffState.ChangeToClean) index = Mathf.Clamp(Mathf.FloorToInt(elapsed / changeSeconds * frames.Length),0,frames.Length-1);
            if (State == CanaStaffState.ChangeBack) index = Mathf.Clamp(reverseFrom - Mathf.FloorToInt(elapsed / changeSeconds * frames.Length),0,frames.Length-1);
            image.sprite = frames[index];
        }

        private void OnDisable()
        {
            CanaAudio.EndMop(this);
            CanaAudio.StopRobotTransform(this);
            if (manager) ReleaseJob();
        }
    }
}
