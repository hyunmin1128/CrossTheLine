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
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!game||!game.settings)return;
            game.RefreshHealthHearts();
            ring.Set(game.FireGauge,game.FeverActive);healthFill.fillAmount=Mathf.Clamp01(game.Health/game.settings.maximumHealth);
            healthText.text="";prayerText.text="";stackText.text="";game.status.text="";
            feverFlame.gameObject.SetActive(game.FeverActive);feverText.gameObject.SetActive(game.FeverActive);
            if(game.FeverActive){feverText.text="피버";if(fireFrames!=null&&fireFrames.Length>0)feverFlame.sprite=fireFrames[Mathf.FloorToInt(Time.unscaledTime*8)%fireFrames.Length];}
        }
    }
}
