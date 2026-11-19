using System;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingObstaclePatterns : MonoBehaviour
    {
        public enum Pattern { Random, Alternating, StaggeredPair, Passage, SandBands }
        [Serializable] public sealed class Choice
        {
            public Pattern pattern;[Min(0)] public float weight=1;
            public Choice(Pattern p,float w){pattern=p;weight=w;}
        }
        public Choice[] choices={new Choice(Pattern.Random,1),new Choice(Pattern.Alternating,3),new Choice(Pattern.StaggeredPair,2),new Choice(Pattern.Passage,2),new Choice(Pattern.SandBands,2)};
        [Min(2)] public float staggerDistance=3.8f;
        public Pattern CurrentPattern{get;private set;}
        public int WaveIndex{get;private set;}
        public void BeginAttack()
        {
            if(choices==null||choices.Length==0){CurrentPattern=Pattern.Random;WaveIndex=0;return;}
            float total=0;foreach(var choice in choices)if(choice!=null&&choice.pattern!=CurrentPattern)total+=Mathf.Max(0,choice.weight);
            if(total<=0){WaveIndex=0;return;}
            float roll=UnityEngine.Random.value*total;
            foreach(var choice in choices)if(choice!=null&&choice.pattern!=CurrentPattern&&choice.weight>0){roll-=choice.weight;if(roll<=0){CurrentPattern=choice.pattern;break;}}
            WaveIndex=0;
        }
        public bool Spawn(FastingTopDownGame game)
        {
            int wave=WaveIndex++;if(CurrentPattern==Pattern.Random)return false;
            int safe=game.encounterDirector?game.encounterDirector.OpenLane:2;
            int[] lanes=new int[4];for(int i=0,n=0;i<5;i++)if(i!=safe)lanes[n++]=i;
            int lane=lanes[wave%4],opposite=lanes[3-wave%4];
            if(!game.TryChooseObstacle(out var moving))return true;
            var fixedKind=moving;
            switch(CurrentPattern)
            {
                case Pattern.Alternating:
                    game.SpawnPatternObstacle(moving,lanes[wave%2==0?wave/2%2:3-wave/2%2],1,0);break;
                case Pattern.StaggeredPair:
                    game.SpawnPatternObstacle(moving,lane,1,0);game.SpawnPatternObstacle(moving,opposite,1,staggerDistance);break;
                case Pattern.Passage:
                    // Keep the same corridor throughout this attack; do not build a closing wall.
                    game.SpawnPatternObstacle(fixedKind,lanes[0],1,0);game.SpawnPatternObstacle(moving,lanes[3],1,staggerDistance);break;
                case Pattern.SandBands:
                    var wind=moving;
                    int count=wind==FastingTopDownHazard.Kind.Sand?(wave%2==0?1:2):1;
                    int[] starts=new int[5];int valid=0;for(int i=0;i<=5-count;i++)if(safe<i||safe>=i+count)starts[valid++]=i;
                    game.SpawnPatternObstacle(wind,starts[wave%valid],count,0);break;
            }
            return true;
        }
    }
}
