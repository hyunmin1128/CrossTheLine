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
            healthText.text=$"체력 {game.Health:0.#} / {game.settings.maximumHealth:0.#}";prayerText.text=$"기도 {game.Prayer:0} / {game.PrayerTarget:0}";
            stackText.text=game.FeverActive?"성령불 피버":$"성령불 {game.FlameStacks} / {game.settings.flamesForFever}";
            game.status.text=game.Prayer>=game.PrayerTarget?"기도 완료 · 금색 전환선으로":game.PrayerBlockRemaining>0?$"기도 방해 {game.PrayerBlockRemaining:0.0}초":game.FeverActive?"성령불 피버 · 기도 가속":"자동 기도 충전 중";
            feverFlame.gameObject.SetActive(game.FeverActive);feverText.gameObject.SetActive(game.FeverActive);
            if(game.FeverActive){feverText.text=$"FEVER {game.FeverRemaining:0.0}초";if(fireFrames!=null&&fireFrames.Length>0)feverFlame.sprite=fireFrames[Mathf.FloorToInt(Time.unscaledTime*8)%fireFrames.Length];}
        }
    }
}
