using UnityEngine;

namespace CrossTheLine.Cana
{
    [System.Serializable]
    public sealed class CanaSeat
    {
        public Transform point;
        public bool facesLeft;
    }

    public sealed class CanaCustomerSpawner : MonoBehaviour
    {
        public CanaCustomer customerPrefab;
        public Transform spawnPoint;
        public Transform moneyPoint;
        public CanaNavigation navigation;
        public CanaSeat[] seats;
        public bool spawnOnStart = true;
        public bool simulateEverySeat = true;
        [Min(0.1f)] public float spawnInterval = 1.5f;
        public SpriteRenderer[] placementPreviews;
        public CanaCustomer ActiveCustomer { get; private set; }
        private CanaCustomer[] occupants;
        private int nextSeat;
        private float nextSpawnTime;

        private void Start()
        {
            foreach (var preview in placementPreviews) if (preview) preview.enabled = false;
            navigation.Rebuild();
            occupants = new CanaCustomer[seats.Length];
            if (spawnOnStart) { SpawnCustomerAtSeat(0); nextSeat = 1; }
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            if (!simulateEverySeat || nextSeat >= seats.Length || Time.time < nextSpawnTime) return;
            if (SpawnCustomerAtSeat(nextSeat)) nextSeat++;
            nextSpawnTime = Time.time + spawnInterval;
        }

        public CanaCustomer SpawnCustomer() => SpawnCustomerAtSeat(0);

        public CanaCustomer SpawnCustomerAtSeat(int index)
        {
            occupants ??= new CanaCustomer[seats.Length];
            if (index < 0 || index >= seats.Length || occupants[index]) return null;
            if (!navigation.TryRoute(spawnPoint.position, seats[index].point.position, out var path))
            {
                Debug.LogError("No route to " + seats[index].point.name, this);
                return null;
            }
            var npc = Instantiate(customerPrefab, path[0], Quaternion.identity);
            npc.name = "NPC1 - " + seats[index].point.name;
            occupants[index] = npc;
            ActiveCustomer = npc;
            npc.Initialize(this, index, path, seats[index].facesLeft);
            return npc;
        }

        public void ReleaseSeat(int index, CanaCustomer npc)
        {
            if (occupants != null && occupants[index] == npc) occupants[index] = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!navigation || !spawnPoint || seats == null) return;
            Gizmos.color = Color.cyan;
            foreach (var seat in seats)
            {
                if (!seat.point || !navigation.TryRoute(spawnPoint.position, seat.point.position, out var path)) continue;
                for (int i = 1; i < path.Length; i++) Gizmos.DrawLine(path[i - 1], path[i]);
            }
        }
    }
}
