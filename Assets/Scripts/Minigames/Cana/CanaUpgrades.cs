using UnityEngine;

namespace CrossTheLine.Cana
{
    public enum CanaUpgradeStatus { Owned, Available, Locked }

    public sealed class CanaUpgrades : MonoBehaviour
    {
        public CanaPlayer player;
        public CanaWallet wallet;
        public CanaShop shop;
        public int[] prices = { 0, 10000, 30000, 0, 8000, 25000 };
        [SerializeField] private int bottleLevel;
        [SerializeField] private int equipmentLevel;
        public int BottleLevel => bottleLevel;
        public int EquipmentLevel => equipmentLevel;
        private readonly bool[] forcedUnlocked = new bool[6];

        public void ForceUnlock(int index) => forcedUnlocked[index] = true;
        public bool CanSteal(int index) => forcedUnlocked[index] && Status(index) == CanaUpgradeStatus.Available;

        private void Awake()
        {
            bottleLevel = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Upgrade.Bottle", 0), 0, 2);
            equipmentLevel = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Upgrade.Equipment", 0), 0, 2);
            Apply();
        }

        public CanaUpgradeStatus Status(int index)
        {
            int level = index < 3 ? bottleLevel : equipmentLevel;
            int tier = index % 3;
            if (tier <= level) return CanaUpgradeStatus.Owned;
            return forcedUnlocked[index] || tier == level + 1 && MeetsRequirement(index) ? CanaUpgradeStatus.Available : CanaUpgradeStatus.Locked;
        }

        private bool MeetsRequirement(int index) => index switch
        {
            1 => wallet.SuccessfulCustomers >= 50,
            2 => wallet.SuccessfulCustomers >= 100,
            4 => shop.TrainingLevel(0) >= 4,
            5 => shop.TrainingLevel(0) >= shop.maxTrainingLevel,
            _ => true
        };

        public string RequirementText(int index)
        {
            if (!MeetsRequirement(index))
                return index switch
                {
                    1 => "Serve 50 guests (" + Mathf.Min(wallet.SuccessfulCustomers, 50) + "/50).",
                    2 => "Serve 100 guests (" + Mathf.Min(wallet.SuccessfulCustomers, 100) + "/100).",
                    4 => "Speed Train: " + shop.TrainingLevel(0) + "/4.",
                    5 => "Max speed Train: " + shop.TrainingLevel(0) + "/" + shop.maxTrainingLevel + ".",
                    _ => ""
                };
            return index == 2 ? "Buy Big Bag first." : index == 5 ? "Buy Shoes first." : "";
        }

        public bool Purchase(int index)
        {
            if (index < 0 || index >= 6 || Status(index) != CanaUpgradeStatus.Available) return false;
            bool stealing = CanSteal(index);
            if (!wallet.TrySpend(shop.UpgradePrice(index))) return false;
            if (index < 3) bottleLevel = index % 3;
            else equipmentLevel = index % 3;
            Apply();
            PlayerPrefs.SetInt("Cana.Upgrade.Bottle", bottleLevel);
            PlayerPrefs.SetInt("Cana.Upgrade.Equipment", equipmentLevel);
            PlayerPrefs.Save();
            if (stealing) shop.OnUpgradeStolen(index);
            return true;
        }

        private void Apply()
        {
            player.bottleTier = (BottleTier)bottleLevel;
            player.movementEquipment = (MovementEquipment)equipmentLevel;
        }
    }
}
