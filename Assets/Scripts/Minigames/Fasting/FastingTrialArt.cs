using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Final Trial Art")]
    public sealed class FastingTrialArt : ScriptableObject
    {
        public Sprite background;
        public FastingTopDownHazard fire,thorns,pillar,reaper;
        [Range(1,3)] public int maximumThornCount=3;
        public float reaperSpeed=5.5f,reaperWarningSeconds=.7f;
        public FastingTopDownHazard PrefabFor(FastingTopDownHazard.Kind kind,FastingTopDownHazard fallback)
        {
            var p=kind==FastingTopDownHazard.Kind.Fire?fire:kind==FastingTopDownHazard.Kind.Thorns?thorns:kind==FastingTopDownHazard.Kind.Pillar?pillar:kind==FastingTopDownHazard.Kind.Reaper?reaper:null;return p?p:fallback;
        }
    }
}
