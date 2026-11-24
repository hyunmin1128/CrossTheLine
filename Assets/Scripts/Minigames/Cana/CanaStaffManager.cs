using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public enum CanaStaffKind { Server, Cleaner, Robot }

    [DefaultExecutionOrder(-500)]
    public sealed class CanaStaffManager : MonoBehaviour
    {
        public CanaShop shop;
        public CanaCustomerSpawner spawner;
        public CanaPartTimer[] workers;
        public Transform winePickup;
        public int[] prices = { 15000, 15000, 40000 };
        private readonly bool[] hired = new bool[3];
        private readonly Dictionary<CanaCustomer, CanaPartTimer> serviceClaims = new();
        private readonly Dictionary<int, CanaPartTimer> cleanClaims = new();
        public bool ServerHired => hired[0];
        public bool CleanerHired => hired[1];
        public bool IsHired(int index) => hired[index];
        public int Price(int index) => prices[index] * (shop.PricesDoubled ? 2 : 1);

        private void Awake()
        {
            for (int i = 0; i < 3; i++) hired[i] = PlayerPrefs.GetInt("Cana.Staff.Hired." + i, 0) == 1;
        }

        private void Start()
        {
            for (int i = 0; i < workers.Length; i++) workers[i].gameObject.SetActive(hired[i]);
        }

        public bool Hire(int index)
        {
            if (index < 0 || index >= 3 || hired[index] || !shop.wallet.TrySpend(Price(index))) return false;
            hired[index] = true;
            PlayerPrefs.SetInt("Cana.Staff.Hired." + index, 1);
            PlayerPrefs.Save();
            workers[index].gameObject.SetActive(true);
            Debug.Log("[Cana Staff] Hired " + (CanaStaffKind)index);
            return true;
        }

        public void Release(CanaPartTimer worker)
        {
            foreach (var key in serviceClaims.Where(pair => pair.Value == worker).Select(pair => pair.Key).ToArray()) serviceClaims.Remove(key);
            foreach (var key in cleanClaims.Where(pair => pair.Value == worker).Select(pair => pair.Key).ToArray()) cleanClaims.Remove(key);
        }

        public bool TryAssign(CanaPartTimer worker)
        {
            var jobs = new List<(CanaCustomer customer, int seat, Vector3 point)>();
            if (worker.kind != CanaStaffKind.Cleaner)
                foreach (var customer in Object.FindObjectsByType<CanaCustomer>())
                    if (customer.IsAwaitingService && (!customer.Party || customer.PartyLeader) &&
                        (!serviceClaims.TryGetValue(customer, out var claimed) || !claimed))
                        jobs.Add((customer, -1, customer.transform.position));
            if (worker.kind != CanaStaffKind.Server)
                for (int i = 0; i < spawner.seats.Length; i++)
                    if (spawner.CanStaffClean(i, worker) && (!cleanClaims.TryGetValue(i, out var claimed) || !claimed))
                        jobs.Add((null, i, spawner.seats[i].point.position));
            foreach (var job in jobs.OrderBy(job => (job.point - worker.transform.position).sqrMagnitude))
            {
                if (!TryApproach(worker.transform.position, job.point, out var path)) continue;
                if (job.customer) serviceClaims[job.customer] = worker;
                else cleanClaims[job.seat] = worker;
                if (worker.Assign(job.customer, job.seat, path)) return true;
                Release(worker);
            }
            return false;
        }

        public bool TryApproach(Vector3 from, Vector3 target, out Vector3[] route)
        {
            var offsets = new[] { new Vector2(.75f,0), new Vector2(-.75f,0), new Vector2(0,.75f),
                new Vector2(0,-.75f), new Vector2(.5f,.5f), new Vector2(-.5f,.5f),
                new Vector2(.5f,-.5f), new Vector2(-.5f,-.5f), Vector2.zero };
            foreach (var offset in offsets.OrderBy(offset => ((Vector2)target + offset - (Vector2)from).sqrMagnitude))
                if (spawner.navigation.TryRoute(from, (Vector2)target + offset, out route)) return true;
            route = System.Array.Empty<Vector3>();
            return false;
        }
    }
}
