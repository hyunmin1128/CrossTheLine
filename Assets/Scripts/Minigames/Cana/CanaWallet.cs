using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaWallet : MonoBehaviour
    {
        private const string SaveKey = "Cana.Gold";
        [SerializeField] private int gold;
        public int Gold => gold;
        public int heartReward = 150;
        public int smileReward = 100;
        public SpriteRenderer moneyBin;
        public CanaGoldPopup popupPrefab;
        public Vector2 popupRandomRange = new Vector2(0.625f, 0.25f);

        private void Awake() => gold = PlayerPrefs.GetInt(SaveKey, 0);

        public void Deposit(CanaReaction reaction)
        {
            int amount = reaction == CanaReaction.Heart ? heartReward :
                reaction == CanaReaction.Smile ? smileReward : 0;
            DepositGold(amount);
        }

        public void DepositGold(int amount)
        {
            if (amount <= 0) return;
            gold += amount;
            Save();
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
            return true;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKey, gold);
            PlayerPrefs.Save();
        }
    }
}
