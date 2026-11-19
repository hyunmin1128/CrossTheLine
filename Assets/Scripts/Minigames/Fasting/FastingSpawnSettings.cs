using System;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    [CreateAssetMenu(menuName="CrossTheLine/Fasting Spawn Settings")]
    public sealed class FastingSpawnSettings : ScriptableObject
    {
        [Min(0),Tooltip("1=현재 빈도, 2=두 배 자주, 0=생성 중지. 공격·휴식 시간과 아이템은 유지합니다.")] public float globalFrequency=1;
        [Serializable] public sealed class Entry {public FastingTopDownHazard.Kind kind;[Min(0)] public float weight=25;}
        [Serializable] public sealed class Stage {public string name;[Min(0)] public float frequency=1;public Entry[] obstacles;}
        public Stage[] stages;
        public float Frequency(int stage)=>Mathf.Max(0,globalFrequency)*(stages!=null&&stage>=0&&stage<stages.Length&&stages[stage]!=null?Mathf.Max(0,stages[stage].frequency):1);
        public bool TryPick(int stage,out FastingTopDownHazard.Kind kind)
        {
            kind=default;if(stages==null||stage<0||stage>=stages.Length||stages[stage]==null||stages[stage].obstacles==null)return false;
            var entries=stages[stage].obstacles;float total=0;foreach(var e in entries)if(e!=null)total+=Mathf.Max(0,e.weight);if(total<=0)return false;
            float roll=UnityEngine.Random.value*total;Entry last=null;
            foreach(var e in entries)if(e!=null&&e.weight>0){last=e;roll-=e.weight;if(roll<0){kind=e.kind;return true;}}
            kind=last.kind;return true;
        }
    }
}
