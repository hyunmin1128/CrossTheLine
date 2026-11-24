using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine.Cana
{
    public enum CanaItemType { None, CleanBomb, BeautyPotion, MonkeySpanner, BlessOfMoses, SoldOut }

    [Serializable]
    public sealed class CanaInventorySlot
    {
        public CanaItemType item;
        public int count;
    }

    public sealed class CanaInventory : MonoBehaviour
    {
        public const int MaxItems = 3;
        public int ItemCount
        {
            get
            {
                int total = 0;
                foreach (var slot in slots) total += slot.count;
                return total;
            }
        }
        public CanaPlayer player;
        public CanaShop shop;
        public CanaCustomerSpawner spawner;
        public CanaRound round;
        public CanaItemEffects effects;
        public Sprite[] itemSprites;
        public SpriteRenderer[] icons;
        public TextMeshPro[] counts;
        public CanaItemDurationOverlay[] durationOverlays;
        private readonly float[] activeStarted = new float[3];
        private readonly float[] activeUntil = new float[3];
        public CanaInventorySlot[] slots = { new CanaInventorySlot(), new CanaInventorySlot(), new CanaInventorySlot() };
        public float beautyDuration = 60, immunityDuration = 30, wineDuration = 30, soldOutDuration = 30;
        private float beautyUntil, immunityUntil, wineUntil, soldOutUntil;
        public bool GoldBoosted => Time.time < beautyUntil;
        public bool ComplaintsBlocked => Time.time < immunityUntil;
        public bool UnlimitedWine => Time.time < wineUntil;
        public bool ArrivalsPaused => Time.time < soldOutUntil;

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].item = (CanaItemType)PlayerPrefs.GetInt("Cana.Item.Type." + i, 0);
                slots[i].count = Mathf.Clamp(PlayerPrefs.GetInt("Cana.Item.Count." + i, 0), 0, 1);
                if ((int)slots[i].item < 1 || (int)slots[i].item > 5 || slots[i].count == 0)
                { slots[i].item = CanaItemType.None; slots[i].count = 0; }
            }
        }

        public bool CanAdd(CanaItemType item) => FindSlot(item) >= 0;

        private int FindSlot(CanaItemType item)
        {
            if (ItemCount >= MaxItems || item == CanaItemType.None || (int)item > 5 || (int)item < 0) return -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i].count == 0 && ActiveRemaining(i) <= 0) return i;
            return -1;
        }

        public bool Purchase(CanaItemType item, int price, CanaWallet wallet)
        {
            int index = FindSlot(item);
            if (index < 0 || !wallet.TrySpend(price)) return false;
            slots[index].item = item;
            slots[index].count = 1;
            Save();
            return true;
        }

        public bool Use(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index].count <= 0 ||
                round.Finished || shop.IsOpen || player.IsCleaning) return false;
            switch (slots[index].item)
            {
                case CanaItemType.CleanBomb: break;
                case CanaItemType.BeautyPotion: beautyUntil = Time.time + beautyDuration; break;
                case CanaItemType.MonkeySpanner: immunityUntil = Time.time + immunityDuration; break;
                case CanaItemType.BlessOfMoses: wineUntil = Time.time + wineDuration; break;
                case CanaItemType.SoldOut: soldOutUntil = Time.time + soldOutDuration; break;
                default: return false;
            }
            effects.Play(slots[index].item);
            if (slots[index].item != CanaItemType.CleanBomb)
            {
                activeStarted[index] = Time.time;
                activeUntil[index] = slots[index].item switch
                {
                    CanaItemType.BeautyPotion => beautyUntil,
                    CanaItemType.MonkeySpanner => immunityUntil,
                    CanaItemType.BlessOfMoses => wineUntil,
                    CanaItemType.SoldOut => soldOutUntil,
                    _ => 0
                };
            }
            if (--slots[index].count == 0 && ActiveRemaining(index) <= 0) slots[index].item = CanaItemType.None;
            Save();
            return true;
        }

        public float ActiveRemaining(int index)
        {
            return Mathf.Max(0, activeUntil[index] - Time.time);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame) Use(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Use(1);
            if (keyboard.digit3Key.wasPressedThisFrame) Use(2);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                float remaining = ActiveRemaining(i);
                bool active = remaining > 0;
                bool occupied = slots[i].count > 0 || active;
                float elapsed = Time.time - activeStarted[i];
                durationOverlays[i].SetProgress(active, active ? elapsed / (elapsed + remaining) : 0);
                if (!active)
                {
                    if (slots[i].count == 0) slots[i].item = CanaItemType.None;
                }
                icons[i].enabled = occupied;
                counts[i].gameObject.SetActive(false);
                if (!occupied) continue;
                icons[i].sprite = itemSprites[(int)slots[i].item - 1];
                var size = icons[i].sprite.rect.size;
                icons[i].transform.position = new Vector3((18 + i * 36 - Mathf.Floor(size.x / 2) - 160) / 16,
                    (90 - 156 - Mathf.Floor(size.y / 2)) / 16, 0);
            }
        }

        private void Save()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                PlayerPrefs.SetInt("Cana.Item.Type." + i, (int)slots[i].item);
                PlayerPrefs.SetInt("Cana.Item.Count." + i, slots[i].count);
            }
            PlayerPrefs.Save();
        }
    }
}
