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
        public bool IsLegend { get; private set; }
        public int FishType { get; private set; }
        public bool LegendWarning { get; private set; }
        public int JourneyLeg => Mathf.Min(3, FishCaught / 50 + 1);
        public bool ReachedMilestone { get; private set; }
        public float ZoneHeight=>IsLegend?settings.legendZoneHeight:settings.catchZoneHeight;
        public float ProgressRequired=>IsLegend?settings.legendProgressRequired:settings.normalProgressRequired;
        public bool Inside=>Mathf.Abs(Fish-Zone)<=ZoneHeight*.5f;
        public string ResultMessage { get; private set; }="";
        float duration,fishTarget,retarget,tapClock=100;
        public NetFishingSession(NetFishingSettings configuration,int seed){settings=configuration;random=new System.Random(seed);}
        float Range(float low,float high)=>low+(high-low)*(float)random.NextDouble();
        void Change(Phase phase){State=phase;PhaseTime=0;}
        public void Start(){if(State==Phase.Introduction)Change(Phase.Exploring);}
        public bool Cast(bool isLegend=false, int fishType=0)
        {
            if(State!=Phase.Exploring)return false;
            IsLegend=isLegend;
            FishType=fishType;
            LegendWarning=false;
            ReachedMilestone=false;
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
            FightTime+=dt;
            UpdateFish(dt);
            float halfZone=ZoneHeight*.5f;
            Zone=Mathf.Clamp(Zone-settings.zoneFallSpeed*dt,halfZone,1-halfZone);
            if(tap&&tapClock>=settings.minimumTapInterval)
            {
                tapClock=0;Zone=Mathf.Min(1-halfZone,Zone+settings.tapLift);
                if(Inside)Progress+=settings.progressPerGoodTap;
                else{Progress-=settings.badTapProgressLoss;Integrity-=settings.badTapNetDamage;}
            }
            if(!Inside){Progress-=settings.progressLossPerSecond*dt;Integrity-=settings.netDamagePerSecond*dt;}
            Progress=Mathf.Clamp(Progress,0,ProgressRequired);Integrity=Mathf.Clamp01(Integrity);
            if(Integrity<=0){BrokenNets++;ResultMessage="그물이 찢어졌습니다 · 새 그물로 교체 중";Change(Phase.Replacing);}
            else if(Progress>=ProgressRequired)
            {
                int low=Mathf.Max(1,settings.minimumCatch),high=Mathf.Max(low,settings.maximumCatch);
                LastCatch=IsLegend?settings.legendCatch:Mathf.Min(random.Next(low,high+1),settings.goalFish-FishCaught);
                int previousLeg=JourneyLeg;
                FishCaught+=LastCatch;
                ReachedMilestone=JourneyLeg>previousLeg&&FishCaught<settings.goalFish;
                ResultMessage=IsLegend?$"Legend 포획! · {LastCatch}마리":$"{LastCatch}마리 포획!";
                Change(Phase.Result);
            }
            else if(Progress<=0)
            {
                Escapes++;ResultMessage="물고기를 놓쳤습니다";Change(Phase.Result);
            }
        }

        void UpdateFish(float dt)
        {
            float speed=settings.fishSpeed;
            if(IsLegend)
            {
                float phase=Mathf.Repeat(FightTime,2.8f);
                LegendWarning=phase>=1.4f&&phase<1.95f;
                if(LegendWarning)return;
                bool up=Mathf.FloorToInt(FightTime/2.8f)%2==0;
                float target=phase<1.4f?(up?.22f:.78f):(up?.88f:.12f);
                speed*=settings.legendFishSpeedMultiplier*(phase>=1.95f?1.75f:1f);
                Fish=Mathf.MoveTowards(Fish,target,speed*dt);
                return;
            }

            if(FishType==1)
            {
                float phase=Mathf.Repeat(FightTime,3.6f);
                if(phase<.85f)return;
                Fish=Mathf.MoveTowards(Fish,phase<1.65f?.84f:.24f,speed*(phase<1.65f?1.65f:.8f)*dt);
                return;
            }

            retarget-=dt;
            if(retarget<=0)
            {
                retarget=Range(settings.fishRetargetSeconds.x,settings.fishRetargetSeconds.y);
                if(FishType==2)fishTarget=Range(.13f,.40f);
                else if(FishType==3)
                {
                    fishTarget=Fish>=.5f?Range(.22f,.44f):Range(.56f,.78f);
                    retarget*=.6f;
                }
                else fishTarget=Range(.24f,.76f);
            }
            if(FishType==0)speed*=.8f;
            Fish=Mathf.MoveTowards(Fish,fishTarget,speed*dt);
        }
    }
}
