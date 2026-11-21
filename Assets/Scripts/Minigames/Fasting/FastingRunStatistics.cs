using System;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        public FastingObstaclePatterns obstaclePatterns;
        public float DirectPrayerSeconds{get;private set;}
        public int DamageHitCount{get;private set;}
        public int AvoidedHazards{get;private set;}
        readonly int[] damageByObstacle=new int[14];
        public int CurrentDay=>Mathf.Clamp(StageIndex*10+Mathf.FloorToInt(Prayer/PrayerTarget*10),StageIndex*10+1,StageIndex*10+10);
        const string BestDayKey="CrossTheLine.Fasting.BestDay",PreviousDayKey="CrossTheLine.Fasting.PreviousDay";
        public void SpawnPatternObstacle(FastingTopDownHazard.Kind kind,int lane,int count,float offset)=>SpawnAbove(kind,lane,count,offset,true);
        void ResetRunStatistics(){DirectPrayerSeconds=0;DamageHitCount=AvoidedHazards=0;Array.Clear(damageByObstacle,0,damageByObstacle.Length);}
        void RecordDamage(string cause)
        {
            DamageHitCount++;if(Enum.TryParse(cause,true,out FastingTopDownHazard.Kind kind)&&(int)kind>=0&&(int)kind<damageByObstacle.Length){damageByObstacle[(int)kind]++;CurrentBalance.hits[(int)kind]++;}
        }
        public static string ObstacleName(FastingTopDownHazard.Kind kind)
        {
            string[] names={"돌멩이","모래바람","선인장","모래지옥","박쥐","낙석","균열","해골무덤","벽","오염된 우물","용암 구멍","가시덤불","화염 기둥","사신"};int i=(int)kind;return i>=0&&i<names.Length?names[i]:"장애물";
        }
        string BuildRunResult(bool completed)
        {
            ExportBalance(completed?"completed":"failed");if(caveVisionMask)caveVisionMask.Clear();
            int day=completed?40:CurrentDay,best=PlayerPrefs.GetInt(BestDayKey,0),previous=PlayerPrefs.GetInt(PreviousDayKey,0),index=-1;
            for(int i=0;i<damageByObstacle.Length;i++)if(damageByObstacle[i]>0&&(index<0||damageByObstacle[i]>damageByObstacle[index]))index=i;
            string most=index>=0?ObstacleName((FastingTopDownHazard.Kind)index)+" · "+damageByObstacle[index]+"회":"없음";
            string progress=previous>0&&day>previous?$"지난 도전보다 {day-previous}일 더 전진했습니다":$"최고 도달 {Mathf.Max(best,day)}일 / 40일";
            PlayerPrefs.SetInt(BestDayKey,Mathf.Max(best,day));PlayerPrefs.SetInt(PreviousDayKey,day);PlayerPrefs.Save();
            return (completed?"40일 금식 기도 여정 완료":"잠시 쉬어 다시 걸어가세요")+$"\n도달 {day}/40일 · 여정 {Elapsed:0}초\n직접 기도 {DirectPrayerSeconds:0.0}초 · 피한 장애물 {AvoidedHazards}개\n피격 {DamageHitCount}회 · 가장 많이 맞은 것: {most}\n{progress}\nR 또는 아래 버튼으로 다시 시작";
        }
    }
}
