using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Top Down Settings")]
    public sealed class FastingTopDownSettings : ScriptableObject
    {
        public float maximumHealth=5, damage=1, moveSpeed=2.5f, stageLength=24;
        public float cactusDamage=2,quicksandStunSeconds=2;
        public float finishLineDistance=6;
        public float prayerTarget=90, prayerPerSecond=1f;
        public float[] prayerTargets={90,100,110,120};
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
        public float bibleSpawnChance=.01f,pageSpawnChance=.10f,pieceSpawnChance=.18f;
        public float[] bibleChances={.01f,.01f,.01f,.01f},pageChances={.10f,.12f,.14f,.16f},pieceChances={.18f,.20f,.22f,.24f};
        public float flameChance=.30f,shieldChance=.16f;
        public float[] wellChances={.65f,.60f,.55f,0},wellIntervalSeconds={18,20,22,24};
        public float[] attackSeconds={5,5.5f,6,6.5f},restSeconds={3,2.8f,2.6f,2.4f};
        public float StageValue(float[] values,int stage,float fallback)=>values!=null&&stage>=0&&stage<values.Length?values[stage]:fallback;
        public float StageDropChance(FastingTopDownEntity.Kind kind,int stage)=>Mathf.Clamp01(kind==FastingTopDownEntity.Kind.Book?StageValue(bibleChances,stage,bibleSpawnChance):kind==FastingTopDownEntity.Kind.Page?StageValue(pageChances,stage,pageSpawnChance):kind==FastingTopDownEntity.Kind.Fragment?StageValue(pieceChances,stage,pieceSpawnChance):kind==FastingTopDownEntity.Kind.Water?StageValue(wellChances,stage,0):kind==FastingTopDownEntity.Kind.Flame?flameChance:kind==FastingTopDownEntity.Kind.Shield?shieldChance:pickupChance);
        public float[] stageMoveSpeeds={2.5f,2.75f,3f,3.25f};
        public float manualMoveSpeed=5.2f,feverSeconds=5;
        public float prayerMoveMultiplier=.15f,scrollSpeedMultiplier=1.15f,hazardSpeedMultiplier=1.2f,sideSandSpeed=7.2f;
        [Min(0)] public float prayerPreparationSeconds=.3f;
        public int flamesForFever=3;
        public float StageSpeed(int stage)=>stageMoveSpeeds!=null&&stage>=0&&stage<stageMoveSpeeds.Length?stageMoveSpeeds[stage]:moveSpeed;
        public float DropChance(FastingTopDownEntity.Kind kind)=>StageDropChance(kind,0);
        public float[] waveIntervals={1.35f,1.25f,1.15f,1f};
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
