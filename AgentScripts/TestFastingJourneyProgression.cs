using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingJourneyProgression
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");g.enabled=false;int count=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);count++;};
        string[] keys={"CrossTheLine.Fasting.BestDay","CrossTheLine.Fasting.PreviousDay"};bool[] exists=keys.Select(PlayerPrefs.HasKey).ToArray();int[] values=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        try
        {
            g.Begin();for(int stage=0;stage<4;stage++)
            {
                ok(g.StageIndex==stage,"Correct stage after transition");foreach(var e in g.entities)e.gameObject.SetActive(false);
                typeof(FastingEncounterDirector).GetField("nextWave",flags).SetValue(g.encounterDirector,100f);
                typeof(FastingEncounterDirector).GetField("remaining",flags).SetValue(g.encounterDirector,100f);
                for(int i=0;i<8;i++)g.Tick(.1f,Vector2.zero,true);
                float prayerSeconds=g.DirectPrayerSeconds;ok(prayerSeconds>.4f*(stage+1),"Prayer duration persists across stages");
                g.Collect(g.entities.First(e=>e.stage==stage&&e.kind==FastingTopDownEntity.Kind.Book));ok(g.Prayer==g.PrayerTarget&&g.GateVisible,"Full scripture still fills current stage and opens gate");
                g.player.position=new Vector3(0,g.gate.position.y);g.Tick(.01f,Vector2.zero);for(int n=0;n<7;n++)g.Tick(.1f,Vector2.zero);
                if(stage<3){ok(g.Running&&g.StageIndex==stage+1&&g.Prayer==0,"Next stage loads and resets only stage prayer");ok(g.DirectPrayerSeconds>=prayerSeconds,"Statistics not erased by stage load");}
            }
            ok(g.Complete&&!g.Running&&g.resultPanel.activeSelf,"Final stage completes");ok(g.result.text.Contains("40일 금식 기도 여정 완료"),"Completion uses run result");
            g.result.ForceMeshUpdate();ok(g.result.preferredHeight<=g.result.rectTransform.rect.height,"Completion text fits");g.retryButton.onClick.Invoke();ok(g.Running&&g.StageIndex==0&&g.DirectPrayerSeconds==0,"Retry button starts clean run");
            return count+" checks passed: all four stages, gate transitions, persistent statistics, completion and retry button.";
        }
        finally{for(int i=0;i<keys.Length;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
