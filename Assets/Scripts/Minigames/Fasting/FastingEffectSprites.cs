using UnityEngine;

namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Effect Sprites")]
    public sealed class FastingEffectSprites : ScriptableObject
    {
        public Sprite[] shieldBreak,bubble,water,prayer,rocks,thorns,sand;
        public Sprite spark;
        [Header("Aseprite 편집용 · 파일 저장 후 Unity가 직접 다시 불러옵니다")]
        public bool editableAuraSprites;
        public Sprite[] prayerAura;
        public Sprite orbitShield;
        [Range(0,1),Tooltip("예수님 뒤에서 표시되는 보호막의 선명도입니다. 원본 파일은 바꾸지 않습니다.")] public float shieldBubbleOpacity=.72f;
        [Range(0,1)] public float prayerAuraOpacity=.52f;
        public static Sprite Frame(Sprite[] frames,int index)=>frames!=null&&frames.Length>0?frames[Mathf.Clamp(index,0,frames.Length-1)]:null;
        public Sprite Particle(FastingTopDownHazard.Kind kind,int index)
        {
            var frames=kind==FastingTopDownHazard.Kind.Cactus||kind==FastingTopDownHazard.Kind.Thorns?thorns:kind==FastingTopDownHazard.Kind.Sand||kind==FastingTopDownHazard.Kind.Quicksand?sand:rocks;
            return frames!=null&&frames.Length>0?frames[index%frames.Length]:null;
        }
    }
}
