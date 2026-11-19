using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Ruins Art")]
    public sealed class FastingRuinsArt : ScriptableObject
    {
        public Sprite background;
        public FastingTopDownHazard rubble,wall,tomb,poisonWell;
        public FastingTopDownHazard verticalWall;
        public bool largeMazeWalls=true;
        [Range(1,1.6f)] public float obstacleScale=1.3f,wallThicknessScale=1.2f;
        [Range(2,4)] public int maximumTombCount=4;
        [Range(2,5)] public int maximumWallSegments=4;
        public FastingTopDownHazard PrefabFor(FastingTopDownHazard.Kind kind,FastingTopDownHazard fallback)
        {
            var p=kind==FastingTopDownHazard.Kind.Rock?rubble:kind==FastingTopDownHazard.Kind.Wall?wall:kind==FastingTopDownHazard.Kind.Tomb?tomb:kind==FastingTopDownHazard.Kind.Poison?poisonWell:null;
            return p?p:fallback;
        }
    }
}
