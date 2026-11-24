using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaDissatisfaction : MonoBehaviour
    {
        public SpriteRenderer gauge;
        public CanaInventory inventory;
        public CanaStaffManager staff;
        public Sprite[] frames;
        [SerializeField] private int complaints;
        [SerializeField] private bool gameOver;
        public int Complaints => complaints;
        public bool IsGameOver => gameOver;
        public int Capacity => frames.Length;

        private void Awake()
        {
            gauge.enabled = complaints > 0;
            if (complaints > 0) gauge.sprite = frames[complaints - 1];
        }

        public void AddComplaint(int amount = 1)
        {
            if (inventory && inventory.ComplaintsBlocked) return;
            if (staff && staff.CleanerHired && Random.value < .25f) return;
            for (int i = 0; i < amount; i++) AddOne();
        }

        private void AddOne()
        {
            if (gameOver) return;
            if (complaints >= Capacity) gameOver = true;
            else complaints++;
            gauge.enabled = complaints > 0;
            if (complaints > 0) gauge.sprite = frames[complaints - 1];
        }

    }
}
