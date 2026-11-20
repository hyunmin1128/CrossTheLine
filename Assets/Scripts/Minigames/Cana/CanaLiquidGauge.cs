using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaLiquidGauge : MonoBehaviour
    {
        public CanaPlayer player;
        public SpriteRenderer gauge;
        public Sprite[] water, wine, bigWater, bigWine;
        public int rowsPerUnit = 3;
        public Vector3 offset = new Vector3(0, 2.125f, 0);

        private void LateUpdate()
        {
            gauge.enabled = !player.InfiniteWine && player.LiquidAmount > 0;
            if (!gauge.enabled) return;
            bool large = player.bottleTier == BottleTier.BigJar;
            bool converted = player.Contents == DrinkContents.Wine;
            var frames = large ? (converted ? bigWine : bigWater) : (converted ? wine : water);
            int index = Mathf.Clamp(Mathf.FloorToInt(player.LiquidAmount * rowsPerUnit + 0.0001f), 0, frames.Length - 1);
            gauge.sprite = frames[index];
            transform.position = player.transform.position + offset;
        }
    }
}
