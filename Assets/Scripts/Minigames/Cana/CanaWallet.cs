using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaWallet : MonoBehaviour
    {
        private const string SaveKey = "Cana.Gold";
        [SerializeField] private int gold;
        public int Gold => gold;
        [SerializeField] private int successfulCustomers;
        public int SuccessfulCustomers => successfulCustomers;
        public CanaInventory inventory;
        public CanaStaffManager staff;
        public int heartReward = 150;
        public int smileReward = 100;
        public SpriteRenderer moneyBin;
        public CanaGoldPopup popupPrefab;
        public Vector2 popupRandomRange = new Vector2(0.625f, 0.25f);

        private void Awake()
        {
            gold = PlayerPrefs.GetInt(SaveKey, 0);
            successfulCustomers = Mathf.Max(0, PlayerPrefs.GetInt("Cana.SuccessfulCustomers", 0));
        }

        public void RecordSuccessfulCustomer()
        {
            successfulCustomers++;
            PlayerPrefs.SetInt("Cana.SuccessfulCustomers", successfulCustomers);
            PlayerPrefs.Save();
        }

        public void Deposit(CanaReaction reaction)
        {
            int amount = reaction == CanaReaction.Heart ? heartReward :
                reaction == CanaReaction.Smile ? smileReward : 0;
            DepositGold(amount);
        }

        public void DepositGold(int amount)
        {
            if (amount <= 0) return;
            float multiplier = (inventory && inventory.GoldBoosted ? 1.5f : 1) * (staff && staff.ServerHired ? 1.25f : 1);
            amount = Mathf.RoundToInt(amount * multiplier);
            gold += amount;
            Save();
            CanaAudio.Play(CanaSound.Coin);
            var position = new Vector3(moneyBin.bounds.center.x + Random.Range(-popupRandomRange.x, popupRandomRange.x),
                moneyBin.bounds.max.y + 0.375f + Random.Range(0, popupRandomRange.y), 0);
            position.x = Mathf.Round(position.x * 16) / 16;
            position.y = Mathf.Round(position.y * 16) / 16;
            Instantiate(popupPrefab, position, Quaternion.identity).Show(amount);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > gold) return false;
            gold -= amount;
            Save();
            if (amount > 0) CanaAudio.Play(CanaSound.Money);
            return true;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKey, gold);
            PlayerPrefs.Save();
        }
    }
}
