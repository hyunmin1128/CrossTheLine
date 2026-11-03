using UnityEngine;
namespace CrossTheLine
{
    public sealed partial class JourneyGame
    {
        public Transform PlayerRoot=>player;
        void RefreshChapterSprites()
        {
            var definition=JourneyAssets.Load<GameObject>("CrossTheLine/Characters/Jesus")?.GetComponent<JourneyCharacterView>();
            authoredJesusIdle=definition?definition.idle:null;
            var sprites=GetComponent<JourneyChapterSpriteSet>();if(sprites&&sprites.jesusIdle)authoredJesusIdle=sprites.jesusIdle;
        }
        internal void ContinuousHud(string title,string prompt){hudObjective.text=title;hudProgress.text=Chapter.title+" · "+title;hudPrompt.text=prompt;hudPrompt.color=ink;}
        internal void ContinuousWalk(bool moving,float direction)
        {
            playerSprite.enabled=true;playerSprite.color=Color.white;playerSprite.maskInteraction=SpriteMaskInteraction.None;playerSprite.transform.localRotation=Quaternion.identity;
            SetFrame(playerSprite,currentActor=="예수님"?JesusWalkFrame(moving):SpriteFor(currentActor),0);
            if(direction!=0)playerSprite.flipX=direction<0;
        }
        internal void PrepareContinuousExit()
        {
            foreach(var overlay in GetComponentsInChildren<Canvas>(true))if(overlay.renderMode==RenderMode.ScreenSpaceOverlay&&(!sceneBindings||overlay!=sceneBindings.canvas))overlay.gameObject.SetActive(false);
            ContinuousWalk(false,1);
        }
    }
}
