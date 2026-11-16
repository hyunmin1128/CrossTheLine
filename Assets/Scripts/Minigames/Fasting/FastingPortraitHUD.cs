using TMPro;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingPortraitHUD : MonoBehaviour
    {
        public FastingTopDownGame game;
        public FastingFeverRing ring;
        public UnityEngine.UI.Image healthFill,feverFlame;
        public TMP_Text healthText,prayerText,stackText,feverText;
        public Sprite[] fireFrames;
        public UnityEngine.UI.Image prayerState;
        public UnityEngine.UI.Image waterSkin;
        public Color waterAvailableColor=Color.white,waterEmptyColor=new Color(.42f,.42f,.42f,.45f);
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!game||!game.settings)return;
            game.RefreshHealthHearts();
            if(waterSkin){waterSkin.color=game.HasWater?waterAvailableColor:waterEmptyColor;}
            ring.Set(game.FireGauge,game.FeverActive);healthFill.fillAmount=Mathf.Clamp01(game.Health/game.settings.maximumHealth);
            healthText.text="";prayerText.text="";stackText.text="";game.status.text="";
            game.prayerFill.color=game.PrayerBlockRemaining>0?new Color(.45f,.45f,.45f):game.Praying?new Color(1,.82f,.32f):game.PrayerPreparing?new Color(.6f,.65f,.75f):new Color(.3f,.65f,.8f);
            if(prayerState){prayerState.gameObject.SetActive(game.PrayerPreparing||game.Praying||game.PrayerBlockRemaining>0||game.InPrayerWindow);prayerState.fillAmount=game.PrayerPreparing?game.PreparationProgress:1;prayerState.color=game.PrayerBlockRemaining>0?Color.gray:game.Praying?new Color(1,.82f,.32f,.7f+.3f*Mathf.Sin(Time.unscaledTime*7)):game.InPrayerWindow?new Color(.45f,.9f,.8f):new Color(.65f,.7f,.9f);
                if(game.effectSprites&&game.effectSprites.prayer.Length>0){prayerState.sprite=FastingEffectSprites.Frame(game.effectSprites.prayer,game.PrayerPreparing?0:(int)(Time.unscaledTime*5)%4);prayerState.type=UnityEngine.UI.Image.Type.Simple;prayerState.preserveAspect=true;prayerState.color=game.PrayerBlockRemaining>0?new Color(.5f,.5f,.5f,.6f):Color.white;}}
            feverFlame.gameObject.SetActive(game.FeverActive);feverText.gameObject.SetActive(game.FeverActive);
            if(game.FeverActive){feverText.text="피버";if(fireFrames!=null&&fireFrames.Length>0)feverFlame.sprite=fireFrames[Mathf.FloorToInt(Time.unscaledTime*8)%fireFrames.Length];}
        }
    }
}
