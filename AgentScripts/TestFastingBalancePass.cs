using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingBalancePass
{
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g||!g.encounterDirector)throw new Exception("Play balanced minigame");g.enabled=false;var rng=UnityEngine.Random.state;int count=0;Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);count++;};
        try
        {
            for(int stage=0;stage<4;stage++)
            {
                ok(Mathf.Abs(g.settings.StageDropChance(FastingTopDownEntity.Kind.Book,stage)-.01f)<.0001f,"Bible chance stage "+stage);
                ok(Mathf.Abs(g.settings.StageDropChance(FastingTopDownEntity.Kind.Page,stage)-(.10f+.02f*stage))<.0001f,"Page chance stage "+stage);
                ok(Mathf.Abs(g.settings.StageDropChance(FastingTopDownEntity.Kind.Fragment,stage)-(.18f+.02f*stage))<.0001f,"Piece chance stage "+stage);
                g.Begin();Set(g,"<StageIndex>k__BackingField",stage);typeof(FastingTopDownGame).GetMethod("ApplyStage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{false});
                foreach(var e in g.entities)e.gameObject.SetActive(false);
                for(int i=0;i<1500;i++)
                {
                    Set(g,"grace",999f);g.Tick(.05f,Vector2.zero,false);
                    if(g.InPrayerWindow)
                    {
                        ok(g.Hazards.All(h=>!h||h.spent||Mathf.Abs(h.velocity.x)<.01f&&h.transform.position.y+h.size.y*.5f+1<=g.player.position.y+.001f),"Clear forward prayer window stage "+stage);
                        break;
                    }
                    if(i==1499)throw new Exception("No prayer window stage "+stage);
                }
                int wave=(int)g.GetType().GetField("waveNumber",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g);
                for(int i=0;i<20;i++)g.Tick(.05f,Vector2.zero,true);
                ok(wave==(int)g.GetType().GetField("waveNumber",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g),"No spawns in prayer window stage "+stage);
            }
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);
            var flame=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Flame);for(int n=0;n<3;n++)g.Collect(flame);
            float timer=g.FeverRemaining;g.Tick(.1f,Vector2.zero,false);ok(g.FeverRemaining==timer,"Fever held while not praying");for(int i=0;i<5;i++)g.Tick(.1f,Vector2.zero,true);ok(g.FeverRemaining<timer,"Fever consumes during prayer");
            var ui=UnityEngine.Object.FindAnyObjectByType<FastingPortraitHUD>();ui.Refresh();ok(ui.prayerState!=null&&!ui.prayerState.raycastTarget,"Editable nonblocking state indicator");ok(ui.prayerText.text=="","No prayer numbers");
            g.Begin();for(int i=0;i<5;i++){Set(g,"grace",0f);g.TryDamage("failure test");}ok(!g.Running&&g.resultPanel.activeSelf,"Failure waits for player restart");float elapsed=g.Elapsed;g.Tick(3,Vector2.zero);ok(g.Health==0&&g.Elapsed==elapsed,"No forced auto restart");g.Begin();ok(g.Running&&g.Health==5,"Retry starts new run");
            var lanes=new System.Collections.Generic.HashSet<int>();foreach(int seed in Enumerable.Range(0,100)){UnityEngine.Random.InitState(seed);g.Begin();foreach(var e in g.entities.Where(e=>e.gameObject.activeSelf))lanes.Add(Mathf.RoundToInt(e.transform.position.x/3));}ok(lanes.Count==5,"Rewards distributed over all five lanes");
            return count+" checks passed: all-stage chances/windows, fever, UI, retry, lane distribution.";
        }
        finally{UnityEngine.Random.state=rng;g.Begin();g.enabled=true;}
    }
}
