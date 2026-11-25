using UnityEngine;

namespace CrossTheLine.Minigames.Fishing
{
    [CreateAssetMenu(menuName="CrossTheLine/Minigames/Net Fishing Settings")]
    public sealed class NetFishingSettings : ScriptableObject
    {
        [Header("목표와 포획 수")]
        [Min(1)] public int goalFish=150;
        [Min(1)] public int minimumCatch=1,maximumCatch=15;
        [Header("배와 스팟")]
        [Min(.1f)] public float boatSpeed=3.2f;
        public Rect boatBounds=new Rect(-7.3f,-2.6f,14.6f,5.7f);
        [Min(.1f)] public float castRadius=1.35f;
        [Min(0)] public float castSeconds=1.1f;
        public Vector2 biteDelay=new Vector2(2,4);
        [Min(.1f)] public float biteNoticeSeconds=.8f;
        [Header("물고기와 연타 조작")]
        [Min(1),Tooltip("그물을 던지는 순간부터 포획까지의 최대 시간. 투척·입질 대기도 포함합니다.")] public float maximumCatchSeconds=30;
        [Range(.05f,.8f)] public float catchZoneHeight=.34f;
        [Min(.01f)] public float tapLift=.2f;
        [Min(.01f)] public float zoneFallSpeed=.52f;
        [Min(.03f)] public float minimumTapInterval=.1f;
        [Range(.01f,.9f)] public float startingProgress=.28f;
        [Min(0)] public float progressPerGoodTap=.0155f;
        [Min(0)] public float progressLossPerSecond=.035f;
        [Min(0)] public float badTapProgressLoss=.003f;
        [Min(.05f)] public float fishSpeed=.27f;
        public Vector2 fishRetargetSeconds=new Vector2(.7f,1.5f);
        [Header("Legend 물고기")]
        [Range(0,1)] public float legendChance=.0025f;
        [Min(1)] public int legendCatch=25;
        [Range(.05f,.8f)] public float legendZoneHeight=.24f;
        [Min(1)] public float legendFishSpeedMultiplier=1.55f;
        [Min(.1f)] public float legendProgressRequired=1f;
        [Min(.1f)] public float normalProgressRequired=.90f;
        [Header("그물 손상과 교체")]
        [Min(0)] public float netDamagePerSecond=.035f;
        [Min(0)] public float badTapNetDamage=.012f;
        [Min(0)] public float replacementSeconds=6;
        [Min(.1f)] public float resultSeconds=2;
    }
}
