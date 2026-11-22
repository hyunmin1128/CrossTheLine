using System;
using UnityEngine;

namespace CrossTheLine.Minigames
{
    [DisallowMultipleComponent]
    [AddComponentMenu("CrossTheLine/Fasting/Stage Configuration")]
    public sealed class FastingStageConfiguration : MonoBehaviour
    {
        [Serializable] public sealed class Obstacle
        {
            public FastingTopDownHazard.Kind kind;
            [Min(0), Tooltip("상대 가중치입니다. 0이면 무작위 선택에서 제외됩니다. 20/30/50이면 각각 20/30/50%입니다.")] public float spawnWeight=25;
            [Min(0), Tooltip("체력 칸 단위. 0.5 같은 소수도 사용할 수 있습니다.")] public float damage=1;
        }

        [Header("스테이지")]
        [Range(0,3)] public int stageIndex;
        public string displayName;
        public Sprite background;
        [Header("장애물 생성과 피해량")]
        [Tooltip("모든 스테이지의 공통 배율은 이 에셋의 Global Frequency에서 수정합니다.")] public FastingSpawnSettings sharedSpawnSettings;
        [Min(0), Tooltip("공통 배율에 곱해지는 이 스테이지의 스폰 배율. 0=중지, 2=두 배.")] public float obstacleFrequency=1;
        [Tooltip("배경 장식 위로 생성하지 않을 가로 범위. 0~1 화면 비율입니다.")] public Vector2 roadEdges=new Vector2(.18f,.82f);
        public Obstacle[] obstacles;
        [Min(.1f)] public float waveInterval=.8f;
        [Min(.1f)] public float attackSeconds=5,restSeconds=3;
        [Min(1)] public float patternIntervalMultiplier=1.8f;
        public Vector2 randomSpawnIntervals=new Vector2(.85f,1.65f);
        [Min(.01f), Tooltip("생성 간격에 곱합니다. 작을수록 자주 생성됩니다.")] public float spawnIntervalScale=.75f;

        [Header("기도와 말씀 아이템 충전")]
        [Min(1)] public float prayerTarget=90;
        [Min(0)] public float prayerPerSecond=1;
        [Range(0,1), Tooltip("목표 기도량에 대한 비율. 1=100%.")] public float bibleCharge=1;
        [Range(0,1)] public float pageCharge=.25f,fragmentCharge=.1f;
        [Header("아이템 생성 확률 · 0~1")]
        [Range(0,1)] public float bibleChance=.01f,pageChance=.1f,fragmentChance=.18f;
        [Range(0,1)] public float flameChance=.3f,shieldChance=.16f,waterChance=.65f;
        [Min(.1f)] public float waterInterval=18;
        [Header("피버 중 불꽃 추가 충전")]
        [Min(0),Tooltip("불꽃 1개당 추가되는 피버 시간(초). 최대 피버 시간까지 충전합니다.")] public float feverRefillSeconds=2;

        [Header("이동과 장애물 속도")]
        [Min(0)] public float scrollSpeed=2.5f;
        [Min(0)] public float playerMoveSpeed=5.2f;
        [Range(0,1)] public float prayerMoveMultiplier=.15f;
        [Min(0)] public float hazardSpeedMultiplier=1.2f;

        [Header("나머지 설정 · 이 에셋을 선택해 수정")]
        [Tooltip("체력, 무적 시간, 상태이상, 피버, 생성 간격 변동 등. 위에 노출된 항목은 이 컴포넌트의 값이 우선입니다. 스테이지마다 별도 에셋입니다.")]
        public FastingTopDownSettings advancedSettings;
        public FastingDesertObstacleArt desertArt;
        public FastingCaveObstacleArt caveArt;
        public FastingRuinsArt ruinsArt;
        public FastingTrialArt trialArt;

        public float DamageFor(FastingTopDownHazard.Kind kind,float fallback)
        {
            if(obstacles!=null)foreach(var entry in obstacles)if(entry!=null&&entry.kind==kind)return Mathf.Max(0,entry.damage);
            return fallback;
        }
        internal void Apply(FastingTopDownGame game)
        {
            var settings=game.settings;int i=game.StageIndex;
            if(advancedSettings)JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(advancedSettings),settings);
            // Existing consumers keep using the same runtime settings object.
            settings.places[i]=displayName;
            settings.prayerTargets[i]=Mathf.Max(1,prayerTarget);settings.prayerPerSecond=Mathf.Max(0,prayerPerSecond);
            settings.pageFractions[i]=Mathf.Clamp01(pageCharge);settings.fragmentFractions[i]=Mathf.Clamp01(fragmentCharge);
            settings.bibleChances[i]=Mathf.Clamp01(bibleChance);settings.pageChances[i]=Mathf.Clamp01(pageChance);settings.pieceChances[i]=Mathf.Clamp01(fragmentChance);
            settings.flameChance=Mathf.Clamp01(flameChance);settings.shieldChance=Mathf.Clamp01(shieldChance);
            settings.feverRefillSeconds=Mathf.Max(0,feverRefillSeconds);
            settings.wellChances[i]=Mathf.Clamp01(waterChance);settings.wellIntervalSeconds[i]=Mathf.Max(.1f,waterInterval);
            settings.stageMoveSpeeds[i]=Mathf.Max(0,scrollSpeed);settings.manualMoveSpeed=Mathf.Max(0,playerMoveSpeed);
            settings.prayerMoveMultiplier=Mathf.Clamp01(prayerMoveMultiplier);settings.hazardSpeedMultiplier=Mathf.Max(0,hazardSpeedMultiplier);
            settings.waveIntervals[i]=Mathf.Max(.1f,waveInterval);settings.attackSeconds[i]=Mathf.Max(.1f,attackSeconds);settings.restSeconds[i]=Mathf.Max(.1f,restSeconds);
            if(game.encounterDirector){game.encounterDirector.patternIntervalMultiplier=Mathf.Max(1,patternIntervalMultiplier);game.encounterDirector.randomIntervals=randomSpawnIntervals;}
            if(desertArt)game.desertArt=desertArt;if(caveArt)game.caveArt=caveArt;if(ruinsArt)game.ruinsArt=ruinsArt;if(trialArt)game.trialArt=trialArt;
            if(game.spawnSettings)
            {
                if(sharedSpawnSettings)game.spawnSettings.globalFrequency=sharedSpawnSettings.globalFrequency;
                var entries=new FastingSpawnSettings.Entry[obstacles==null?0:obstacles.Length];
                for(int n=0;n<entries.Length;n++)entries[n]=new FastingSpawnSettings.Entry{kind=obstacles[n].kind,weight=Mathf.Max(0,obstacles[n].spawnWeight)};
                game.spawnSettings.stages[i]=new FastingSpawnSettings.Stage{name=displayName,frequency=Mathf.Max(0,obstacleFrequency),roadEdges=roadEdges,obstacles=entries};
            }
        }
    }
}
