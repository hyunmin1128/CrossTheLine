using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CrossTheLine.Cana
{
    public sealed class CanaShop : MonoBehaviour
    {
        [Header("Shop testing")]
        public bool openAtStart;
        public bool enableTestTabShortcut = true;
        public GameObject visuals;
        public CanaPlayer player;
        public SpriteRenderer tabSelect;
        public SpriteRenderer tabFocusSelect;
        public SpriteRenderer trainSelect;
        public GameObject trainContent;
        public Animator judasAnimator;
        public CanaJudasReaction judas;
        public string surprisedDialogue = "What?! You stole from me?!";
        public string lockedItemDialogue = "That item is locked, my friend.";
        public string goldLockDialogue = "Not a chance this time!";
        public bool PricesDoubled { get; private set; }
        public int TrainingPrice(int index) => prices[index] * (PricesDoubled ? 2 : 1);
        public int ItemPrice(int index) => itemPrices[index] * (PricesDoubled ? 2 : 1);
        public int UpgradePrice(int index) => upgrades.CanSteal(index) ? 0 : upgrades.prices[index] * (PricesDoubled ? 2 : 1);

        public void OnLockBroken(int index)
        {
            upgrades.ForceUnlock(index);
            previousUpgradeStates[index] = CanaUpgradeStatus.Available;
            statusMessage = null;
        }

        public void OnUpgradeStolen(int index)
        {
            PricesDoubled = true;
            foreach (var unlock in upgradeUnlocks)
                if (unlock && upgrades.Status(unlock.upgradeIndex) == CanaUpgradeStatus.Locked) unlock.Reinforce();
            judas.PlaySurprise();
            statusMessage = null;
            Debug.Log("[Cana Shop] Upgrade stolen: " + index + "; other prices doubled.");
        }
        public TextMeshPro speech;
        [Header("Part-time staff")]
        public CanaStaffManager staff;
        public GameObject staffContent;
        public SpriteRenderer staffSelect;
        public TextMeshPro[] staffPrices;
        public SpriteRenderer[] staffCoins;
        public int PartTimeIndex { get; private set; }
        public string[] staffDescriptions = { "Serves wine. Earn 25% more gold.", "Cleans seats and calms guests.", "Serves or cleans the nearest job." };
        private bool pendingStaff;
        [Header("Item purchases")]
        public CanaInventory inventory;
        public GameObject itemContent;
        public GameObject speechBubble;
        public SpriteRenderer itemSelect;
        public TextMeshPro[] itemPriceLabels;
        public int[] itemPrices = { 1500, 2500, 2000, 3000, 2000 };
        public string[] itemDescriptions = {
            "Clear every dirty seat.",
            "Earn 50% more gold for 60 seconds.",
            "No dissatisfaction for 30 seconds.",
            "Endless wine for 30 seconds.",
            "Stop new arrivals for 30 seconds."
        };
        public int ItemIndex { get; private set; }
        private bool pendingItem;

        [Header("Upgrade purchases")]
        public CanaUpgrades upgrades;
        public GameObject upgradeContent;
        public SpriteRenderer upgradeSelect;
        public SpriteRenderer[] upgradeIcons;
        public GameObject[] upgradeLocks;
        public CanaUpgradeUnlock[] upgradeUnlocks;
        public SpriteRenderer[] upgradeCoins;
        public SpriteRenderer bagNowSelect, equipmentNowSelect;
        public TextMeshPro[] upgradePrices;
        public int UpgradeIndex { get; private set; }
        public string[] upgradeDescriptions = {
            "Bag: holds five servings.",
            "Big Bag: holds ten servings.",
            "Special Bag: endless wine.",
            "A humble pair of bare feet.",
            "Leather shoes. Q to dash.",
            "Press Q to teleport."
        };
        private bool pendingUpgrade;
        private readonly CanaUpgradeStatus[] previousUpgradeStates = new CanaUpgradeStatus[6];
        private int lastSelectionKey = -1;
        private float selectionStarted;

        [Min(0.1f)] public float blinkPeriod = 1;
        [Header("Training purchases")]
        public CanaWallet wallet;
        public TextMeshPro balanceLabel;
        public TextMeshPro[] priceLabels;
        public int[] prices = { 500, 500, 500, 500 };
        public string[] descriptions = {
            "Movement speed +10%.",
            "Cleaning speed +10%.",
            "Less slowdown with a full bag.",
            "Water filling speed +10%."
        };
        public string[] finalTrainingDescriptions = {
            "Final: double movement speed.",
            "Final: clean in 0.1 seconds.",
            "Final: only 1% load slowdown.",
            "Final: fill any bag in 0.1s."
        };
        private string TrainingDescription(int index) => TrainingLevel(index) >= maxTrainingLevel - 1 ?
            finalTrainingDescriptions[index] : index == 2 ?
            "Full bag slowdown: -" + Mathf.RoundToInt((0.5f - (TrainingLevel(index) + 1) * 0.05f) * 100) + "%." : descriptions[index];
        public SpriteRenderer[] trainingGauges;
        public int maxTrainingLevel = 8;
        public GameObject confirmation;
        public TextMeshPro confirmationPrompt;
        public SpriteRenderer yesSelect, noSelect;
        public bool Confirming { get; private set; }
        public bool ConfirmYes { get; private set; }
        [SerializeField] private int[] trainingLevels = new int[4];
        public int TrainingLevel(int index) => trainingLevels[index];
        private int pendingTrain;
        private string statusMessage;
        private int describedTrain = -1;

        public UnityEvent<int> onTrainSelected = new UnityEvent<int>();
        public int TabIndex { get; private set; }
        public int TrainIndex { get; private set; }
        public int LastSelectedTrain { get; private set; } = -1;
        public bool InTrainList { get; private set; }
        public bool IsOpen { get; private set; }
        private float previousTimeScale;
        private bool playerWasEnabled;

        private void Start()
        {
            for (int i = 0; i < 4; i++)
                trainingLevels[i] = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Shop.Train." + i, 0), 0, maxTrainingLevel);
            for (int i = 0; i < 6; i++) previousUpgradeStates[i] = upgrades.Status(i);
            if (openAtStart) OpenShop();
            else visuals.SetActive(false);
        }

        public void OpenShop()
        {
            if (IsOpen) return;
            previousTimeScale = Time.timeScale;
            playerWasEnabled = player.enabled;
            player.enabled = false;
            Time.timeScale = 0;
            IsOpen = true;
            TabIndex = TrainIndex = ItemIndex = UpgradeIndex = PartTimeIndex = 0;
            InTrainList = true;
            Confirming = false;
            statusMessage = null;
            describedTrain = -1;
            lastSelectionKey = -1;
            confirmation.SetActive(false);
            judasAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            visuals.SetActive(true);
            Refresh();
        }

        public void CloseShop()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Confirming = false;
            confirmation.SetActive(false);
            visuals.SetActive(false);
            Time.timeScale = previousTimeScale;
            if (player) player.enabled = playerWasEnabled;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (enableTestTabShortcut && keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                if (IsOpen) CloseShop();
                else OpenShop();
                return;
            }
            if (!IsOpen) return;
            if (judas.IsSurprised) { Refresh(); return; }
            if (keyboard != null)
            {
                bool up = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
                bool down = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
                bool left = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                bool right = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
                if (Confirming)
                {
                    if (left || right) { ConfirmYes = !ConfirmYes; CanaAudio.Play(CanaSound.Pop); }
                    if (keyboard.spaceKey.wasPressedThisFrame) ResolvePurchase();
                    Refresh();
                    return;
                }
                if (InTrainList && TabIndex == 3)
                {
                    if (left) InTrainList = false;
                    PartTimeIndex = (PartTimeIndex + (down ? 1 : 0) - (up ? 1 : 0) + 3) % 3;
                }
                else if (InTrainList && TabIndex == 2) NavigateUpgrades(left, right, up, down);
                else if (InTrainList && TabIndex == 1) NavigateItems(left, right, up, down);
                else
                {
                    if (left) InTrainList = false;
                    int delta = (down ? 1 : 0) - (up ? 1 : 0);
                    if (InTrainList) TrainIndex = (TrainIndex + delta + 4) % 4;
                    else TabIndex = (TabIndex + delta + 4) % 4;
                    if (right) InTrainList = true;
                }
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    if (!InTrainList) { CanaAudio.Play(CanaSound.Bubble); InTrainList = true; }
                    else if (InTrainList)
                    {
                        BeginPurchase();
                    }
                }
            }
            Refresh();
        }

        private void NavigateItems(bool left, bool right, bool up, bool down)
        {
            int column = ItemIndex % 2;
            if (left)
            {
                if (column == 0) { InTrainList = false; return; }
                ItemIndex--;
            }
            else if (right && column == 0 && ItemIndex + 1 < 5) ItemIndex++;
            if (up || down)
            {
                int row = Mathf.Clamp(ItemIndex / 2 + (down ? 1 : -1), 0, 2);
                ItemIndex = Mathf.Min(row * 2 + ItemIndex % 2, 4);
            }
        }

        private void NavigateUpgrades(bool left, bool right, bool up, bool down)
        {
            int column = UpgradeIndex % 3;
            if (left)
            {
                if (column == 0) { InTrainList = false; return; }
                UpgradeIndex--;
            }
            else if (right && column < 2) UpgradeIndex++;
            if (up) UpgradeIndex %= 3;
            if (down) UpgradeIndex = UpgradeIndex % 3 + 3;
        }

        private void BeginPurchase()
        {
            CanaAudio.Play(CanaSound.Bubble);
            pendingItem = TabIndex == 1;
            pendingUpgrade = TabIndex == 2;
            pendingStaff = TabIndex == 3;
            if (pendingStaff)
            {
                if (staff.IsHired(PartTimeIndex)) { statusMessage = "Already in your service."; return; }
                pendingTrain = PartTimeIndex;
                Confirming = ConfirmYes = true;
                confirmation.SetActive(true);
                return;
            }
            if (pendingUpgrade)
            {
                var state = upgrades.Status(UpgradeIndex);
                if (state == CanaUpgradeStatus.Owned)
                {
                    upgrades.SelectOwned(UpgradeIndex);
                    statusMessage = null;
                    return;
                }
                if (state != CanaUpgradeStatus.Available)
                {
                    if (state == CanaUpgradeStatus.Locked && upgradeUnlocks[UpgradeIndex] &&
                        upgradeUnlocks[UpgradeIndex].PlayDenied())
                    {
                        judas.PlayLook();
                        statusMessage = PricesDoubled ? goldLockDialogue : lockedItemDialogue;
                    }
                    return;
                }
                pendingTrain = UpgradeIndex;
                Confirming = ConfirmYes = true;
                confirmation.SetActive(true);
                return;
            }
            if (pendingItem)
            {
                if (!inventory.CanAdd((CanaItemType)(ItemIndex + 1)))
                {
                    statusMessage = "Your pack is full, my friend.";
                    return;
                }
                pendingTrain = ItemIndex;
                Confirming = ConfirmYes = true;
                confirmation.SetActive(true);
                return;
            }
            if (trainingLevels[TrainIndex] >= maxTrainingLevel)
            {
                statusMessage = "This lesson is complete.";
                return;
            }
            pendingTrain = TrainIndex;
            Confirming = true;
            ConfirmYes = true;
            confirmation.SetActive(true);
        }

        private void ResolvePurchase()
        {
            CanaAudio.Play(CanaSound.Bubble);
            if (ConfirmYes)
            {
                int index = pendingTrain;
                if (pendingStaff)
                {
                    bool hired = staff.Hire(index);
                    if (hired) judas.PlayBuy();
                    statusMessage = hired ? "A helper for your gold." : "A full purse first, my friend.";
                }
                else if (pendingUpgrade)
                {
                    bool stealing = upgrades.CanSteal(index);
                    bool bought = upgrades.Purchase(index);
                    if (bought && !stealing) judas.PlayBuy();
                    statusMessage = bought ? "A wise choice, my friend." : "A full purse first, my friend.";
                }
                else if (pendingItem)
                {
                    bool bought = inventory.Purchase((CanaItemType)(index + 1), ItemPrice(index), wallet);
                    if (bought) judas.PlayBuy();
                    statusMessage = bought ? "A wise choice, my friend." :
                        inventory.CanAdd((CanaItemType)(index + 1)) ? "A full purse first, my friend." : "Your pack is full, my friend.";
                }
                else if (trainingLevels[index] < maxTrainingLevel && wallet.TrySpend(TrainingPrice(index)))
                {
                    trainingLevels[index]++;
                    PlayerPrefs.SetInt("Cana.Shop.Train." + index, trainingLevels[index]);
                    PlayerPrefs.Save();
                    LastSelectedTrain = index;
                    onTrainSelected.Invoke(index);
                    judas.PlayBuy();
                    statusMessage = "A wise choice, my friend.";
                }
                else statusMessage = "A full purse first, my friend.";
            }
            Confirming = false;
            confirmation.SetActive(false);
        }

        private void Refresh()
        {
            balanceLabel.text = wallet.Gold.ToString();
            for (int i = 0; i < 4; i++)
            {
                priceLabels[i].text = PriceText(prices[i], TrainingPrice(i));
                for (int slot = 0; slot < maxTrainingLevel; slot++)
                    trainingGauges[i * maxTrainingLevel + slot].enabled = slot < trainingLevels[i];
            }
            int descriptionKey = TabIndex * 10 + (TabIndex == 3 ? PartTimeIndex : TabIndex == 2 ? UpgradeIndex : TabIndex == 1 ? ItemIndex : TrainIndex);
            if (describedTrain != descriptionKey)
            {
                describedTrain = descriptionKey;
                statusMessage = null;
            }
            bool showSpeech = InTrainList;
            if (TabIndex == 2 && previousUpgradeStates[UpgradeIndex] != upgrades.Status(UpgradeIndex))
                statusMessage = null;
            speechBubble.SetActive(showSpeech);
            speech.gameObject.SetActive(showSpeech);
            speech.text = InTrainList ? statusMessage ?? (TabIndex == 0 ? TrainingDescription(TrainIndex) :
                TabIndex == 1 ? itemDescriptions[ItemIndex] : TabIndex == 2 ? upgradeDescriptions[UpgradeIndex] : staffDescriptions[PartTimeIndex]) : "Hello.";
            if (TabIndex == 2 && InTrainList && upgrades.Status(UpgradeIndex) == CanaUpgradeStatus.Locked)
                speech.text = statusMessage ?? upgrades.RequirementText(UpgradeIndex);
            bool lockReply = TabIndex == 2 && InTrainList && upgrades.Status(UpgradeIndex) == CanaUpgradeStatus.Locked && statusMessage != null;
            if (judas.IsSurprised && !lockReply) speech.text = surprisedDialogue;
            staffContent.SetActive(TabIndex == 3);
            staffSelect.enabled = TabIndex == 3 && InTrainList;
            staffSelect.transform.localPosition = Vector3.down * (PartTimeIndex * 40f / 16);
            for (int i = 0; i < 3; i++)
            {
                bool hired = staff.IsHired(i);
                staffPrices[i].text = hired ? "Hired" : PriceText(staff.prices[i], staff.Price(i));
                if (staffCoins != null && i < staffCoins.Length && staffCoins[i]) staffCoins[i].enabled = !hired;
            }
            upgradeContent.SetActive(TabIndex == 2);
            upgradeSelect.enabled = TabIndex == 2 && InTrainList;
            upgradeSelect.transform.localPosition = new Vector3(UpgradeIndex % 3 * 53f / 16, -(UpgradeIndex / 3) * 52f / 16, 0);
            for (int i = 0; i < 6; i++)
            {
                var state = upgrades.Status(i);
                var unlock = upgradeUnlocks[i];
                if (state == CanaUpgradeStatus.Available && previousUpgradeStates[i] == CanaUpgradeStatus.Locked && unlock)
                    unlock.Play();
                previousUpgradeStates[i] = state;
                bool animating = unlock && unlock.IsPlaying;
                upgradeIcons[i].enabled = state != CanaUpgradeStatus.Locked && (!animating || unlock.IsRevealing);
                upgradeLocks[i].SetActive(state == CanaUpgradeStatus.Locked || animating);
                upgradeCoins[i].enabled = state == CanaUpgradeStatus.Available && !upgrades.CanSteal(i);
                upgradePrices[i].text = state == CanaUpgradeStatus.Owned || state == CanaUpgradeStatus.Locked || upgrades.CanSteal(i) ? "" : PriceText(upgrades.prices[i], UpgradePrice(i));
            }
            if (bagNowSelect) bagNowSelect.transform.localPosition = Vector3.right * (upgrades.SelectedBottleLevel * 53f / 16);
            if (equipmentNowSelect) equipmentNowSelect.transform.localPosition = Vector3.right * (upgrades.SelectedEquipmentLevel * 53f / 16);
            itemContent.SetActive(TabIndex == 1);
            itemSelect.enabled = TabIndex == 1 && InTrainList;
            itemSelect.transform.localPosition = new Vector3(ItemIndex % 2 * 76f / 16, -(ItemIndex / 2) * 35f / 16, 0);
            for (int i = 0; i < 5; i++) itemPriceLabels[i].text = PriceText(itemPrices[i], ItemPrice(i));
            yesSelect.enabled = Confirming && ConfirmYes;
            noSelect.enabled = Confirming && !ConfirmYes;
            if (Confirming) confirmationPrompt.text = pendingStaff ? "Would you like to hire them?" : pendingUpgrade && upgrades.CanSteal(pendingTrain) ?
                "Would you like to steal this?" : "Would you like to buy this?";
            tabSelect.transform.localPosition = Vector3.down * (TabIndex * 15f / 16);
            tabFocusSelect.transform.localPosition = tabSelect.transform.localPosition;
            tabFocusSelect.enabled = !InTrainList;
            trainContent.SetActive(TabIndex == 0);
            trainSelect.enabled = TabIndex == 0 && InTrainList;
            trainSelect.transform.localPosition = Vector3.down * (TrainIndex * 30f / 16);
            int selectionKey = descriptionKey + (InTrainList ? 0 : 1000);
            if (selectionKey != lastSelectionKey)
            {
                if (lastSelectionKey >= 0) CanaAudio.Play(CanaSound.Pop);
                lastSelectionKey = selectionKey;
                selectionStarted = Time.unscaledTime;
            }
            float alpha = (Mathf.Cos((Time.unscaledTime - selectionStarted) * Mathf.PI * 2 / blinkPeriod) + 1) / 2;
            trainSelect.color = new Color(1, 1, 1, alpha);
            itemSelect.color = new Color(1, 1, 1, alpha);
            upgradeSelect.color = new Color(1, 1, 1, alpha);
            staffSelect.color = new Color(1, 1, 1, alpha);
        }

        private void OnDisable()
        {
            if (IsOpen) CloseShop();
        }

        private static string PriceText(int original, int current) => original == current ? original.ToString() :
            "<color=#70594A><s>" + original + "</s></color> <color=#70211B>" + current + "</color>";
    }
}
