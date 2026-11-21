using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        [Header("스테이지 프리팹 · 사막 / 동굴 / 폐유적 / 마지막 시련")]
        public FastingStageConfiguration[] stageConfigurations;
        FastingTopDownSettings ownedStageSettings;
        FastingSpawnSettings ownedSpawnSettings;
        public FastingStageConfiguration CurrentStageConfiguration=>stageConfigurations!=null&&StageIndex>=0&&StageIndex<stageConfigurations.Length?stageConfigurations[StageIndex]:null;
        void InitializeStageConfigurations()
        {
            if(stageConfigurations==null||stageConfigurations.Length==0)return;
            ownedStageSettings=Instantiate(settings);ownedStageSettings.name="Fasting Runtime Settings";settings=ownedStageSettings;
            if(spawnSettings){ownedSpawnSettings=Instantiate(spawnSettings);spawnSettings=ownedSpawnSettings;}
        }
        void ReleaseStageConfigurations(){if(ownedStageSettings)Destroy(ownedStageSettings);if(ownedSpawnSettings)Destroy(ownedSpawnSettings);}
        void ApplyStageConfiguration(){if(CurrentStageConfiguration)CurrentStageConfiguration.Apply(this);}
        float DamageFor(string cause)
        {
            float fallback=cause==nameof(FastingTopDownHazard.Kind.Cactus)?settings.cactusDamage:cause==nameof(FastingTopDownHazard.Kind.Crack)?settings.crackDamage:cause==nameof(FastingTopDownHazard.Kind.Poison)?settings.poisonDamage:cause==nameof(FastingTopDownHazard.Kind.Pillar)?settings.pillarDamage:settings.damage;
            return CurrentStageConfiguration&&System.Enum.TryParse(cause,out FastingTopDownHazard.Kind kind)?CurrentStageConfiguration.DamageFor(kind,fallback):fallback;
        }
    }
}
