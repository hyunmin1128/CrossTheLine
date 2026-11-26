using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaParty : MonoBehaviour
    {
        public CanaCustomer[] members = new CanaCustomer[4];
        public int Served { get; private set; }
        public bool Ordering { get; private set; }
        public bool Resolved { get; private set; }
        private int arrived, drank;
        private float started;
        private CanaReaction reaction;
        private bool departureTrashCreated;

        public void LeaveSeatsDirty()
        {
            if (departureTrashCreated) return;
            departureTrashCreated = true;
            foreach (var member in members)
                if (member) member.MarkSeatDirty();
        }

        public void Arrived()
        {
            arrived++;
            if (arrived != 4) return;
            started = Time.time;
            Ordering = true;
            foreach (var member in members) member.ShowOrder();
        }

        private void Update()
        {
            if (!Ordering || Resolved || !members[0] || members[0].RoundFinished) return;
            float seconds = Time.time - started;
            foreach (var member in members) member.ShowPatience(seconds);
            if (seconds >= members[0].WaitDuration) Fail();
        }

        public bool Serve(CanaPlayer player)
        {
            if (!Ordering || Resolved) return false;
            if (Time.time - started >= members[0].WaitDuration) { Fail(); return false; }
            if (!player.TryServeWine()) return false;
            return AcceptServing();
        }

        public bool ServeFromWorker()
        {
            if (!Ordering || Resolved) return false;
            if (Time.time - started >= members[0].WaitDuration) { Fail(); return false; }
            return AcceptServing();
        }

        private bool AcceptServing()
        {
            CanaAudio.Play(CanaSound.Serve);
            Served++;
            foreach (var member in members) member.SetRequest(4 - Served);
            if (Served == 4)
            {
                Ordering = false;
                reaction = (Time.time - started) / members[0].secondsPerGauge <= members[0].lastGreenFrame + 1
                    ? CanaReaction.Heart : CanaReaction.Smile;
                foreach (var member in members) member.StartDrinking(reaction);
            }
            return true;
        }

        public void Drank()
        {
            if (++drank != 4) return;
            Resolved = true;
            foreach (var member in members) member.React(reaction);
        }

        private void Fail()
        {
            Ordering = false;
            Resolved = true;
            foreach (var member in members) member.React(CanaReaction.Angry);
        }

        private void LateUpdate()
        {
            if (Resolved && System.Array.TrueForAll(members, member => !member)) Destroy(gameObject);
        }
    }
}
