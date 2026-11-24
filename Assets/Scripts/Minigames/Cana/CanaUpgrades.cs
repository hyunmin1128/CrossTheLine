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
        [SerializeField] private int selectedBottleLevel;
        [SerializeField] private int selectedEquipmentLevel;
        public int SelectedBottleLevel => selectedBottleLevel;
        public int SelectedEquipmentLevel => selectedEquipmentLevel;
        public bool IsSelected(int index) => index < 3 ? index == selectedBottleLevel : index - 3 == selectedEquipmentLevel;
        private const string OwnedSaveKey = "Cana.Upgrade.Owned";
        [SerializeField] private int ownedUpgradeMask = (1 << 0) | (1 << 3);
        private bool Owns(int index) => index >= 0 && index < 6 && (ownedUpgradeMask & (1 << index)) != 0;
        private readonly bool[] forcedUnlocked = new bool[6];

        public void ForceUnlock(int index) { if (!shop.PricesDoubled) forcedUnlocked[index] = true; }
        public bool CanSteal(int index) => !shop.PricesDoubled && forcedUnlocked[index] && Status(index) == CanaUpgradeStatus.Available;

        private void Awake()
        {
            bottleLevel = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Upgrade.Bottle", 0), 0, 2);
            equipmentLevel = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Upgrade.Equipment", 0), 0, 2);
            // Older saves only recorded the highest normally purchased tier.
            int legacyMask = ((1 << (bottleLevel + 1)) - 1) | (((1 << (equipmentLevel + 1)) - 1) << 3);
            ownedUpgradeMask = (PlayerPrefs.GetInt(OwnedSaveKey, legacyMask) & 63) | 9;
            selectedBottleLevel = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Upgrade.SelectedBottle", bottleLevel), 0, bottleLevel);
            selectedEquipmentLevel = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Upgrade.SelectedEquipment", equipmentLevel), 0, equipmentLevel);
            if (!Owns(selectedBottleLevel)) selectedBottleLevel = 0;
            if (!Owns(selectedEquipmentLevel + 3)) selectedEquipmentLevel = 0;
            Apply();
        }

        public CanaUpgradeStatus Status(int index)
        {
            if (index < 0 || index >= 6) return CanaUpgradeStatus.Locked;
            if (Owns(index)) return CanaUpgradeStatus.Owned;
            bool forced = forcedUnlocked[index] && !shop.PricesDoubled;
            bool normal = Owns(index - 1) && MeetsRequirement(index);
            return forced || normal ? CanaUpgradeStatus.Available : CanaUpgradeStatus.Locked;
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
            ownedUpgradeMask |= 1 << index;
            if (index < 3) bottleLevel = Mathf.Max(bottleLevel, index % 3);
            else equipmentLevel = Mathf.Max(equipmentLevel, index % 3);
            if (index < 3) selectedBottleLevel = index % 3;
            else selectedEquipmentLevel = index % 3;
            Apply();
            PlayerPrefs.SetInt(OwnedSaveKey, ownedUpgradeMask);
            PlayerPrefs.SetInt("Cana.Upgrade.Bottle", bottleLevel);
            PlayerPrefs.SetInt("Cana.Upgrade.Equipment", equipmentLevel);
            SaveSelection();
            if (stealing) shop.OnUpgradeStolen(index);
            return true;
        }

        public bool SelectOwned(int index)
        {
            if (index < 0 || index >= 6 || Status(index) != CanaUpgradeStatus.Owned) return false;
            if (index < 3) selectedBottleLevel = index;
            else selectedEquipmentLevel = index - 3;
            Apply();
            SaveSelection();
            return true;
        }

        private void SaveSelection()
        {
            PlayerPrefs.SetInt("Cana.Upgrade.SelectedBottle", selectedBottleLevel);
            PlayerPrefs.SetInt("Cana.Upgrade.SelectedEquipment", selectedEquipmentLevel);
            PlayerPrefs.Save();
        }

        private void Apply()
        {
            player.bottleTier = (BottleTier)selectedBottleLevel;
            player.movementEquipment = (MovementEquipment)selectedEquipmentLevel;
        }
    }
}
