using System;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Minigames/Fasting settings")]
    public sealed class FastingSettings : ScriptableObject
    {
        [Serializable] public class Difficulty
        {
            public string label;
            [Min(5)] public float seconds=22;
            [Min(1)] public float windSpeed=5;
            [Min(1)] public float waveInterval=5;
            [Range(1,2)] public int dangerousLanes=1;
            [Min(.8f)] public float warningSeconds=1.3f;
            [Min(4)] public float exposureSeconds=12;
        }
        public Difficulty[] stages=new Difficulty[8];
        public Rect ground=new Rect(-13.5f,-7.2f,85,2.2f);
        public float[] lanes={-5f,-6.1f,-7.2f};
        public float stepWidth=1.5f,stepSeconds=.23f,stageLength=9;
        public int maximumStamina=5;
        public float hitGraceSeconds=1.2f,restartDelay=1.8f;
    }
}
