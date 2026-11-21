using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        public FastingCaveObstacleArt caveArt;
        public FastingRuinsArt ruinsArt;
        public FastingTrialArt trialArt;
        Sprite[] authoredBackgrounds;Vector3[] authoredBackgroundScales;
        void ApplyStageBackgrounds(Color tint)
        {
            if(authoredBackgrounds==null)
            {
                authoredBackgrounds=new Sprite[backgrounds.Length];authoredBackgroundScales=new Vector3[backgrounds.Length];
                for(int i=0;i<backgrounds.Length;i++)if(backgrounds[i]){authoredBackgrounds[i]=backgrounds[i].sprite;authoredBackgroundScales[i]=backgrounds[i].transform.localScale;}
            }
            for(int i=0;i<backgrounds.Length;i++)
            {
                var bg=backgrounds[i];if(!bg)continue;bool cave=StageIndex==1&&caveArt&&caveArt.background;
                bool ruins=StageIndex==2&&ruinsArt&&ruinsArt.background;
                bool trial=StageIndex==3&&trialArt&&trialArt.background;
                bg.sprite=cave?caveArt.background:ruins?ruinsArt.background:trial?trialArt.background:authoredBackgrounds[i];bg.color=cave||ruins||trial?Color.white:tint;
                if(CurrentStageConfiguration&&CurrentStageConfiguration.background){bg.sprite=CurrentStageConfiguration.background;bg.color=Color.white;}
                var size=authoredBackgrounds[i].bounds.size;var target=bg.sprite.bounds.size;
                bg.transform.localScale=Vector3.Scale(authoredBackgroundScales[i],new Vector3(size.x/target.x,size.y/target.y,1));
            }
        }
    }
}
