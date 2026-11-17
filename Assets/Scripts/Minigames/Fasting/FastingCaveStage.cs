using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        public FastingCaveObstacleArt caveArt;
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
                bg.sprite=cave?caveArt.background:authoredBackgrounds[i];bg.color=cave?Color.white:tint;
                var size=authoredBackgrounds[i].bounds.size;var target=bg.sprite.bounds.size;
                bg.transform.localScale=Vector3.Scale(authoredBackgroundScales[i],new Vector3(size.x/target.x,size.y/target.y,1));
            }
        }
    }
}
