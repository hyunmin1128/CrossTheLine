using UnityEngine;

namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Effect Sprites")]
    public sealed class FastingEffectSprites : ScriptableObject
    {
        public Sprite[] shieldBreak,bubble,water,prayer,rocks,thorns,sand;
        public Sprite spark;
        public static Sprite Frame(Sprite[] frames,int index)=>frames!=null&&frames.Length>0?frames[Mathf.Clamp(index,0,frames.Length-1)]:null;
        public Sprite Particle(FastingTopDownHazard.Kind kind,int index)
        {
            var frames=kind==FastingTopDownHazard.Kind.Cactus||kind==FastingTopDownHazard.Kind.Thorns?thorns:kind==FastingTopDownHazard.Kind.Sand||kind==FastingTopDownHazard.Kind.Quicksand?sand:rocks;
            return frames!=null&&frames.Length>0?frames[index%frames.Length]:null;
        }
    }
}
