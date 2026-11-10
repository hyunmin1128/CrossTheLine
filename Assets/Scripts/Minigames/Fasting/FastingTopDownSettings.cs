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
        public float bibleSpawnChance=.03f,pageSpawnChance=.08f,pieceSpawnChance=.10f;
        public float[] stageMoveSpeeds={2.5f,2.75f,3f,3.25f};
        public float manualMoveSpeed=4.5f,feverSeconds=5;
        public int flamesForFever=3;
        public float StageSpeed(int stage)=>stageMoveSpeeds!=null&&stage>=0&&stage<stageMoveSpeeds.Length?stageMoveSpeeds[stage]:moveSpeed;
        public float DropChance(FastingTopDownEntity.Kind kind)=>kind==FastingTopDownEntity.Kind.Book?bibleSpawnChance:kind==FastingTopDownEntity.Kind.Page?pageSpawnChance:kind==FastingTopDownEntity.Kind.Fragment?pieceSpawnChance:pickupChance;
        public float[] waveIntervals={2.4f,2f,1.7f,1.4f};
        public Vector2 spawnIntervalMultiplier=new Vector2(.75f,1.35f);
        public float NextSpawnInterval(int stage)
        {
            float mean=waveIntervals[Mathf.Clamp(stage,0,waveIntervals.Length-1)];
            float min=Mathf.Max(.8f,mean*Mathf.Min(spawnIntervalMultiplier.x,spawnIntervalMultiplier.y));
            float max=Mathf.Max(min,mean*Mathf.Max(spawnIntervalMultiplier.x,spawnIntervalMultiplier.y));
            return Random.Range(min,max);
        }
        public string[] places={"광야 사막","어두운 동굴","지하 폐유적","마지막 시련"};
    }
}
