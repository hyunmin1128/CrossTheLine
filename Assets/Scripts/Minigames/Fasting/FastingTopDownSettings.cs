using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Top Down Settings")]
    public sealed class FastingTopDownSettings : ScriptableObject
    {
        public float maximumHealth=5, damage=1, moveSpeed=2.5f, stageLength=24;
        public float finishLineDistance=6;
        public float prayerTarget=200, prayerPerSecond=1f;
        public float[] prayerTargets={200,250,300,350};
        public float[] pageFractions={.25f,.30f,.35f,.40f};
        public float[] fragmentFractions={.10f,.15f,.20f,.25f};
        public float ScriptureFraction(int stage,bool page)
        {
            var values=page?pageFractions:fragmentFractions;
            return values!=null&&stage>=0&&stage<values.Length?Mathf.Clamp01(values[stage]):page?.25f:.1f;
        }
        public float GetPrayerTarget(int stage)
        {
            if(prayerTargets!=null&&stage>=0&&stage<prayerTargets.Length&&prayerTargets[stage]>0)return prayerTargets[stage];
            return Mathf.Max(1,prayerTarget);
        }
        public float hitGrace=1.2f, warningSeconds=1.4f, visionSeconds=1.1f, visionOpacity=.42f;
        public float laneSpacing=3, pickupChance=.20f;
        public float[] waveIntervals={3.4f,3f,2.7f,2.4f};
        public string[] places={"광야 사막","어두운 동굴","지하 폐유적","마지막 시련"};
    }
}
