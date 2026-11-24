using UnityEngine;

namespace CrossTheLine.Cana
{
    [DefaultExecutionOrder(-10000)]
    public sealed class CanaShopTestReset : MonoBehaviour
    {
        public bool resetOnStart = true;
        public int startingGold = 500000;

        private void Awake()
        {
            if (!resetOnStart) return;
            for (int i = 0; i < 4; i++) PlayerPrefs.DeleteKey("Cana.Shop.Train." + i);
            for (int i = 0; i < 3; i++)
            {
                PlayerPrefs.DeleteKey("Cana.Item.Type." + i);
                PlayerPrefs.DeleteKey("Cana.Item.Count." + i);
            }
            PlayerPrefs.DeleteKey("Cana.Upgrade.Owned");
            PlayerPrefs.DeleteKey("Cana.Upgrade.Bottle");
            PlayerPrefs.DeleteKey("Cana.Upgrade.Equipment");
            PlayerPrefs.DeleteKey("Cana.Upgrade.SelectedBottle");
            PlayerPrefs.DeleteKey("Cana.Upgrade.SelectedEquipment");
            PlayerPrefs.SetInt("Cana.Gold", startingGold);
            PlayerPrefs.Save();
            Debug.Log("[Cana Shop Test] Reset training, inventory and equipment; gold: " + startingGold);
        }
    }
}
