using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingJourneyPresentation : MonoBehaviour
    {
        public FastingTopDownGame game;
        public UnityEngine.UI.Image atmosphere;
        [Min(12)] public float dayNightSeconds=24;
        public Color daylight=new Color(1,.7f,.3f,.025f),night=new Color(.08f,.17f,.38f,.18f);
        public void Refresh(float dt)
        {
            if(!game)return;
            if(!game.Running){atmosphere.color=Color.clear;return;}
            float phase=Mathf.Repeat(game.Elapsed/Mathf.Max(12,dayNightSeconds),1);
            float dusk=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.55f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,phase)));
            atmosphere.color=Color.Lerp(daylight,night,dusk);
        }
        void LateUpdate(){Refresh(Time.deltaTime);}
        void OnDisable(){if(atmosphere)atmosphere.color=Color.clear;}
    }
}
