using System;
using UnityEngine;

namespace CrossTheLine.Minigames.Fishing
{
    // The same simulation is used by live input and seeded balance tests.
    public sealed class NetFishingSession
    {
        public enum Phase { Introduction, Exploring, Casting, Waiting, Bite, Fighting, Result, Replacing, Complete }
        readonly NetFishingSettings settings;
        readonly System.Random random;
        public Phase State { get; private set; }=Phase.Introduction;
        public int FishCaught { get; private set; }
        public int LastCatch { get; private set; }
        public int Escapes { get; private set; }
        public int BrokenNets { get; private set; }
        public int Casts { get; private set; }
        public float Elapsed { get; private set; }
        public float PhaseTime { get; private set; }
        public float FightTime { get; private set; }
        public float CastElapsed { get; private set; }
        public float Progress { get; private set; }
        public float Integrity { get; private set; }=1;
        public float Zone { get; private set; }=.5f;
        public float Fish { get; private set; }=.5f;
        public bool Inside=>Mathf.Abs(Fish-Zone)<=settings.catchZoneHeight*.5f;
        public string ResultMessage { get; private set; }="";
        float duration,fishTarget,retarget,tapClock=100;
        public NetFishingSession(NetFishingSettings configuration,int seed){settings=configuration;random=new System.Random(seed);}
        float Range(float low,float high)=>low+(high-low)*(float)random.NextDouble();
        void Change(Phase phase){State=phase;PhaseTime=0;}
        public void Start(){if(State==Phase.Introduction)Change(Phase.Exploring);}
        public bool Cast()
        {
            if(State!=Phase.Exploring)return false;
            Casts++;LastCatch=0;CastElapsed=0;Change(Phase.Casting);return true;
        }
        public void Step(float dt,bool tap)
        {
            if(State==Phase.Introduction||State==Phase.Complete)return;
            dt=Mathf.Clamp(dt,0,.1f);Elapsed+=dt;PhaseTime+=dt;tapClock+=dt;
            if(State==Phase.Casting||State==Phase.Waiting||State==Phase.Bite||State==Phase.Fighting)
            {
                CastElapsed+=dt;
                if(CastElapsed>=settings.maximumCatchSeconds){Escapes++;ResultMessage="물고기가 멀어졌습니다 · 다시 스팟을 찾아보세요";Change(Phase.Result);return;}
            }
            switch(State)
            {
                case Phase.Casting:
                    if(PhaseTime>=settings.castSeconds){duration=Range(settings.biteDelay.x,settings.biteDelay.y);Change(Phase.Waiting);}break;
                case Phase.Waiting:
                    if(PhaseTime>=duration)Change(Phase.Bite);break;
                case Phase.Bite:
                    if(PhaseTime>=settings.biteNoticeSeconds)
                    {
                        Zone=Fish=.5f;fishTarget=Range(.15f,.85f);retarget=0;Progress=settings.startingProgress;FightTime=0;tapClock=100;Change(Phase.Fighting);
                    }break;
                case Phase.Fighting:Fight(dt,tap);break;
                case Phase.Result:
                    if(PhaseTime>=settings.resultSeconds)Change(FishCaught>=settings.goalFish?Phase.Complete:Phase.Exploring);break;
                case Phase.Replacing:
                    if(PhaseTime>=settings.replacementSeconds){Integrity=1;Change(Phase.Exploring);}break;
            }
        }
        void Fight(float dt,bool tap)
        {
            FightTime+=dt;retarget-=dt;
            if(retarget<=0){fishTarget=Range(.12f,.88f);retarget=Range(settings.fishRetargetSeconds.x,settings.fishRetargetSeconds.y);}
            Fish=Mathf.MoveTowards(Fish,fishTarget,settings.fishSpeed*dt);
            Zone=Mathf.Clamp(Zone-settings.zoneFallSpeed*dt,settings.catchZoneHeight*.5f,1-settings.catchZoneHeight*.5f);
            if(tap&&tapClock>=settings.minimumTapInterval)
            {
                tapClock=0;Zone=Mathf.Min(1-settings.catchZoneHeight*.5f,Zone+settings.tapLift);
                if(Inside)Progress+=settings.progressPerGoodTap;
                else{Progress-=settings.badTapProgressLoss;Integrity-=settings.badTapNetDamage;}
            }
            if(!Inside){Progress-=settings.progressLossPerSecond*dt;Integrity-=settings.netDamagePerSecond*dt;}
            Progress=Mathf.Clamp01(Progress);Integrity=Mathf.Clamp01(Integrity);
            if(Integrity<=0){BrokenNets++;ResultMessage="그물이 찢어졌습니다 · 새 그물로 교체 중";Change(Phase.Replacing);}
            else if(Progress>=1)
            {
                int low=Mathf.Max(1,settings.minimumCatch),high=Mathf.Max(low,settings.maximumCatch);
                LastCatch=Mathf.Min(random.Next(low,high+1),settings.goalFish-FishCaught);FishCaught+=LastCatch;
                ResultMessage=$"{LastCatch}마리 포획!";Change(Phase.Result);
            }
            else if(Progress<=0)
            {
                Escapes++;ResultMessage="물고기를 놓쳤습니다";Change(Phase.Result);
            }
        }
    }
}
