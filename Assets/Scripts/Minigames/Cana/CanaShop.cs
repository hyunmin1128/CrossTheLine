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
        public TextMeshPro speech;
        [Min(0.1f)] public float blinkPeriod = 1;
        [Header("Training purchases")]
        public CanaWallet wallet;
        public TextMeshPro balanceLabel;
        public TextMeshPro[] priceLabels;
        public int[] prices = { 500, 500, 500, 500 };
        public string[] descriptions = {
            "Gold buys a quicker step.",
            "A little gold, a quicker clean.",
            "More water. No slower steps.",
            "A quicker fill, for a price."
        };
        public SpriteRenderer[] trainingGauges;
        public int maxTrainingLevel = 8;
        public GameObject confirmation;
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
            TabIndex = TrainIndex = 0;
            InTrainList = true;
            Confirming = false;
            statusMessage = null;
            describedTrain = -1;
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
            if (keyboard != null)
            {
                bool up = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
                bool down = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
                bool left = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                bool right = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
                if (Confirming)
                {
                    if (left || right) ConfirmYes = !ConfirmYes;
                    if (keyboard.spaceKey.wasPressedThisFrame) ResolvePurchase();
                    Refresh();
                    return;
                }
                if (left) InTrainList = false;
                int delta = (down ? 1 : 0) - (up ? 1 : 0);
                if (InTrainList) TrainIndex = (TrainIndex + delta + 4) % 4;
                else TabIndex = (TabIndex + delta + 4) % 4;
                if (right && TabIndex == 0) InTrainList = true;
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    if (!InTrainList && TabIndex == 0) InTrainList = true;
                    else if (InTrainList)
                    {
                        BeginPurchase();
                    }
                }
            }
            Refresh();
        }

        private void BeginPurchase()
        {
            if (trainingLevels[TrainIndex] >= maxTrainingLevel)
            {
                statusMessage = "This lesson is complete, my friend.";
                return;
            }
            pendingTrain = TrainIndex;
            Confirming = true;
            ConfirmYes = true;
            confirmation.SetActive(true);
        }

        private void ResolvePurchase()
        {
            if (ConfirmYes)
            {
                int index = pendingTrain;
                if (trainingLevels[index] < maxTrainingLevel && wallet.TrySpend(prices[index]))
                {
                    trainingLevels[index]++;
                    PlayerPrefs.SetInt("Cana.Shop.Train." + index, trainingLevels[index]);
                    PlayerPrefs.Save();
                    LastSelectedTrain = index;
                    onTrainSelected.Invoke(index);
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
                priceLabels[i].text = prices[i].ToString();
                for (int slot = 0; slot < maxTrainingLevel; slot++)
                    trainingGauges[i * maxTrainingLevel + slot].enabled = slot < trainingLevels[i];
            }
            if (describedTrain != TrainIndex)
            {
                describedTrain = TrainIndex;
                statusMessage = null;
            }
            speech.text = InTrainList && TabIndex == 0 ? statusMessage ?? descriptions[TrainIndex] : "Hello.";
            yesSelect.enabled = Confirming && ConfirmYes;
            noSelect.enabled = Confirming && !ConfirmYes;
            tabSelect.transform.localPosition = Vector3.down * (TabIndex * 15f / 16);
            tabFocusSelect.transform.localPosition = tabSelect.transform.localPosition;
            tabFocusSelect.enabled = !InTrainList;
            trainContent.SetActive(TabIndex == 0);
            trainSelect.enabled = TabIndex == 0 && InTrainList;
            trainSelect.transform.localPosition = Vector3.down * (TrainIndex * 30f / 16);
            float alpha = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / blinkPeriod) + 1) / 2;
            trainSelect.color = new Color(1, 1, 1, alpha);
        }

        private void OnDisable()
        {
            if (IsOpen) CloseShop();
        }
    }
}
