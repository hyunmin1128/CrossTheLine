using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Cave Obstacle Art")]
    public sealed class FastingCaveObstacleArt : ScriptableObject
    {
        public Sprite background,stalagmite,fallingRock,crack;
        public Sprite[] batFrames;
        public float batFramesPerSecond=10;
        [Range(1,3)] public int maximumBatCount=3;
        [Range(1,2)] public int maximumFixedCount=2;
        public FastingTopDownHazard stalagmitePrefab,fallingRockPrefab,crackPrefab,batPrefab;
        public FastingTopDownHazard PrefabFor(FastingTopDownHazard.Kind kind,FastingTopDownHazard fallback)
        {
            var p=kind==FastingTopDownHazard.Kind.Rock?stalagmitePrefab:kind==FastingTopDownHazard.Kind.Collapse?fallingRockPrefab:kind==FastingTopDownHazard.Kind.Crack?crackPrefab:kind==FastingTopDownHazard.Kind.Bat?batPrefab:null;return p?p:fallback;
        }
        public void Apply(FastingTopDownHazard h)
        {
            var sprite=h.kind==FastingTopDownHazard.Kind.Rock?stalagmite:h.kind==FastingTopDownHazard.Kind.Collapse?fallingRock:h.kind==FastingTopDownHazard.Kind.Crack?crack:h.kind==FastingTopDownHazard.Kind.Bat&&batFrames!=null&&batFrames.Length>0?batFrames[0]:null;
            if(sprite)h.SetArtwork(sprite);if(h.kind==FastingTopDownHazard.Kind.Bat&&batFrames!=null&&batFrames.Length>0)h.SetAnimation(batFrames,batFramesPerSecond);
        }
    }
}
