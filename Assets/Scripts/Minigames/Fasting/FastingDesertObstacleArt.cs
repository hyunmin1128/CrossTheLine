using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Desert Obstacle Art")]
    public sealed class FastingDesertObstacleArt : ScriptableObject
    {
        public Sprite rock,sandstorm,cactus,quicksand;
        public Sprite[] sandstormFrames;
        public float sandstormFramesPerSecond=8;
        public FastingTopDownHazard rockPrefab,sandstormPrefab,cactusPrefab,quicksandPrefab;
        public FastingTopDownHazard PrefabFor(FastingTopDownHazard.Kind kind,FastingTopDownHazard fallback)
        {
            var prefab=kind==FastingTopDownHazard.Kind.Rock?rockPrefab:kind==FastingTopDownHazard.Kind.Sand?sandstormPrefab:kind==FastingTopDownHazard.Kind.Cactus?cactusPrefab:kind==FastingTopDownHazard.Kind.Quicksand?quicksandPrefab:null;
            return prefab?prefab:fallback;
        }
        public Sprite For(FastingTopDownHazard.Kind kind)=>kind==FastingTopDownHazard.Kind.Rock?rock:kind==FastingTopDownHazard.Kind.Sand?sandstorm:kind==FastingTopDownHazard.Kind.Cactus?cactus:kind==FastingTopDownHazard.Kind.Quicksand?quicksand:null;
        public void Apply(FastingTopDownHazard hazard){var sprite=For(hazard.kind);if(sprite)hazard.SetArtwork(sprite);if(hazard.kind==FastingTopDownHazard.Kind.Sand&&sandstormFrames!=null&&sandstormFrames.Length>0)hazard.SetAnimation(sandstormFrames,sandstormFramesPerSecond);}
    }
}
