using System;
using System.IO;
using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        [Serializable] public sealed class StageBalance
        {
            public float seconds,directPrayer,itemPrayer;
            public int[] selected=new int[14],spawned=new int[14],rejected=new int[14],hits=new int[14],pickups=new int[7];
            public int rejectedForSpace,rejectedForEscape,rejectedForPickups,rejectedForOverlap,suppressedGroups;
        }
        [Serializable] public sealed class BalanceReport
        {
            public string utc,outcome,spawnConfiguration;public string[] stageNames,obstacleNames;public StageBalance[] stages;
        }
        StageBalance[] balanceStages;
        public StageBalance CurrentBalance {get{EnsureBalance();return balanceStages[Mathf.Clamp(StageIndex,0,3)];}}
        void EnsureBalance(){if(balanceStages!=null)return;balanceStages=new StageBalance[4];for(int i=0;i<4;i++)balanceStages[i]=new StageBalance();}
        void ResetBalance(){if(Elapsed>=5)ExportBalance("restarted");balanceStages=null;EnsureBalance();}
        void RegisterHazard(FastingTopDownHazard h){h.minimumVisibleReactionSeconds=settings.minimumVisibleReactionSeconds;hazards.Add(h);CurrentBalance.spawned[(int)h.kind]++;}
        void RecordRejection(FastingTopDownHazard h,string reason="overlap"){var s=CurrentBalance;s.rejected[(int)h.kind]++;s.rejectedForSpace++;if(reason=="escape")s.rejectedForEscape++;else if(reason=="pickup")s.rejectedForPickups++;else s.rejectedForOverlap++;}
        [ContextMenu("테스트 통계 저장")]
        public void ExportBalanceReport()=>ExportBalance("manual");
        public string BalanceReportPath=>Path.Combine(Application.persistentDataPath,"FastingBalance","Latest.json");
        void OnApplicationQuit(){if(Elapsed>=5)ExportBalance("stopped");}
        void ExportBalance(string outcome)
        {
            EnsureBalance();var names=new string[14];for(int i=0;i<14;i++)names[i]=ObstacleName((FastingTopDownHazard.Kind)i);
            var report=new BalanceReport{utc=DateTime.UtcNow.ToString("o"),outcome=outcome,stages=balanceStages,stageNames=settings.places,obstacleNames=names,spawnConfiguration=spawnSettings?JsonUtility.ToJson(spawnSettings):""};
            try{Directory.CreateDirectory(Path.GetDirectoryName(BalanceReportPath));File.WriteAllText(BalanceReportPath,JsonUtility.ToJson(report,true));}catch(IOException e){Debug.LogWarning("통계 저장 실패: "+e.Message);}
        }
    }
}
