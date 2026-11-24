using UnityEngine;

namespace CrossTheLine.Cana
{
    [DefaultExecutionOrder(100)]
    public sealed class CanaItemEffects : MonoBehaviour
    {
        public CanaInventory inventory;
        public CanaPlayer player;
        public Camera gameCamera;
        public SpriteRenderer bomb, beauty, moses, soldOut;
        public Sprite[] bombFrames, beautyFrames, mosesFrames;
        public CanaOneShotEffect.FootAnchor[] footAnchors;
        public float balloonFlightSeconds = 0.575f;
        public Vector2 balloonStartOffset = new Vector2(-12, -7);
        public float balloonApexHeight = 3.5f;
        public float burstFrameSeconds = 0.1f;
        public float waterFlowSeconds = 1.5f;
        public float beautyFramesPerSecond = 8;
        private float bombElapsed, beautyStarted;
        private bool bombDetonated;
        public bool BombPlaying { get; private set; }
        public int BombFrame { get; private set; }
        public Vector3 FootPosition { get; private set; }

        public void Play(CanaItemType item)
        {
            if (item == CanaItemType.CleanBomb)
            {
                bombElapsed = 0;
                bombDetonated = false;
                BombPlaying = true;
            }
            if (item == CanaItemType.BeautyPotion) beautyStarted = Time.time;
        }

        private void LateUpdate()
        {
            UpdateBomb();
            Vector2 foot = new Vector2(player.characterSprite.sprite.bounds.center.x, player.characterSprite.sprite.bounds.min.y);
            foreach (var anchor in footAnchors)
                if (anchor.sprite == player.characterSprite.sprite) { foot = anchor.position; break; }
            if (player.characterSprite.flipX) foot.x = -foot.x;
            if (player.characterSprite.flipY) foot.y = -foot.y;
            FootPosition = player.characterSprite.transform.TransformPoint(foot);
            beauty.enabled = inventory.GoldBoosted;
            if (beauty.enabled)
            {
                beauty.sprite = beautyFrames[Mathf.FloorToInt((Time.time - beautyStarted) * beautyFramesPerSecond) % beautyFrames.Length];
                beauty.transform.position = Snap(FootPosition + Vector3.down * (4f / 16));
                beauty.sortingOrder = player.characterSprite.sortingOrder + 2;
            }
            moses.enabled = inventory.UnlimitedWine;
            if (moses.enabled)
            {
                moses.sprite = mosesFrames[0];
                moses.transform.position = Snap(FootPosition - moses.sprite.bounds.center);
                moses.sortingOrder = player.characterSprite.sortingOrder - 2;
            }
            soldOut.enabled = inventory.ArrivalsPaused;
        }

        private void UpdateBomb()
        {
            bomb.enabled = BombPlaying;
            if (!BombPlaying) return;
            bombElapsed += Time.deltaTime;
            if (!bombDetonated && bombElapsed >= balloonFlightSeconds)
            {
                bombDetonated = true;
                inventory.spawner.ClearAllDirt();
            }
            float flowStart = balloonFlightSeconds + burstFrameSeconds * 2;
            Vector3 anchor = new Vector3(gameCamera.transform.position.x, gameCamera.transform.position.y - 90f / 16, 0);
            if (bombElapsed < balloonFlightSeconds)
            {
                BombFrame = 0;
                anchor += BalloonOffset(bombElapsed);
            }
            else if (bombElapsed < flowStart)
                BombFrame = 1 + Mathf.Min(1, Mathf.FloorToInt((bombElapsed - balloonFlightSeconds) / burstFrameSeconds));
            else
            {
                float flow = (bombElapsed - flowStart) / waterFlowSeconds;
                if (flow >= 1)
                {
                    BombPlaying = false;
                    bomb.enabled = false;
                    return;
                }
                BombFrame = flow < 0.5f ? 3 : 4;
            }
            bomb.sprite = bombFrames[BombFrame];
            bomb.transform.position = Snap(anchor);
        }

        private static Vector3 Snap(Vector3 point) => new Vector3(Mathf.Round(point.x * 16) / 16,
            Mathf.Round(point.y * 16) / 16, point.z);

        private Vector3 BalloonOffset(float seconds)
        {
            float duration = Mathf.Max(0.01f, balloonFlightSeconds);
            float apex = Mathf.Max(balloonApexHeight, balloonStartOffset.y + 0.01f, 0.01f);
            float rise = apex - balloonStartOffset.y;
            float ascentTime = duration * Mathf.Sqrt(rise) / (Mathf.Sqrt(rise) + Mathf.Sqrt(apex));
            float gravity = 2 * rise / (ascentTime * ascentTime);
            float t = Mathf.Clamp(seconds, 0, duration);
            return new Vector3(Mathf.Lerp(balloonStartOffset.x, 0, t / duration),
                balloonStartOffset.y + gravity * ascentTime * t - 0.5f * gravity * t * t, 0);
        }
    }
}
