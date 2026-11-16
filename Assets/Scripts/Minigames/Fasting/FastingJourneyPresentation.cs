using TMPro;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingJourneyPresentation : MonoBehaviour
    {
        public FastingTopDownGame game;
        public UnityEngine.UI.Image atmosphere;
        public CanvasGroup dayBanner;
        public TMP_Text dayLabel;
        [Min(12)] public float dayNightSeconds=24;
        public float bannerSeconds=2;
        public Color daylight=new Color(1,.7f,.3f,.025f),night=new Color(.08f,.17f,.38f,.18f);
        float remaining,lastElapsed=-1;int lastDay=-1;bool wasRunning;
        public void Refresh(float dt)
        {
            if(!game)return;
            if(!game.Running){atmosphere.color=Color.clear;dayBanner.alpha=0;wasRunning=false;return;}
            if(!wasRunning||game.Elapsed<lastElapsed){lastDay=-1;remaining=0;}
            wasRunning=true;lastElapsed=game.Elapsed;
            float phase=Mathf.Repeat(game.Elapsed/Mathf.Max(12,dayNightSeconds),1);
            float dusk=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.55f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,phase)));
            atmosphere.color=Color.Lerp(daylight,night,dusk);
            if(lastDay!=game.CurrentDay){lastDay=game.CurrentDay;remaining=bannerSeconds;dayLabel.text=$"{lastDay}일째 · {game.settings.places[game.StageIndex]}\n<size=70%>금식과 기도의 여정</size>";}
            remaining=Mathf.Max(0,remaining-Mathf.Max(0,dt));float age=bannerSeconds-remaining;
            dayBanner.alpha=Mathf.Min(Mathf.Clamp01(age/.2f),Mathf.Clamp01(remaining/.4f));
        }
        void LateUpdate(){Refresh(Time.deltaTime);}
        void OnDisable(){if(atmosphere)atmosphere.color=Color.clear;if(dayBanner)dayBanner.alpha=0;wasRunning=false;}
    }
}
