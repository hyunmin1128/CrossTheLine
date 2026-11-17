using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingPatternsAndResults
{
    static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Set(object o,string name,object value)=>o.GetType().GetProperty(name).SetValue(o,value);
    static void Field(object o,string name,object value)=>o.GetType().GetField(name,flags).SetValue(o,value);
    static void Quiet(FastingTopDownGame g){foreach(var e in g.entities)e.gameObject.SetActive(false);Field(g.encounterDirector,"nextWave",100f);Field(g.encounterDirector,"nextRandom",100f);Field(g.encounterDirector,"remaining",100f);}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g.obstaclePatterns)throw new Exception("Play updated scene");
        int n=0;Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);n++;};var random=UnityEngine.Random.state;var patterns=g.obstaclePatterns;var choices=patterns.choices;g.enabled=false;
        string[] keys={"CrossTheLine.Fasting.BestDay","CrossTheLine.Fasting.PreviousDay"};bool[] exists=keys.Select(PlayerPrefs.HasKey).ToArray();int[] values=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();
        try
        {
            UnityEngine.Random.InitState(1716);
            foreach(FastingObstaclePatterns.Pattern pattern in Enum.GetValues(typeof(FastingObstaclePatterns.Pattern)))for(int stage=0;stage<4;stage++)for(int safe=0;safe<5;safe++)
            {
                g.Begin();Quiet(g);Set(g,"StageIndex",stage);Set(g.encounterDirector,"OpenLane",safe);Set(patterns,"CurrentPattern",pattern);Set(patterns,"WaveIndex",0);
                for(int wave=0;wave<4;wave++)g.SpawnWave();
                ok(g.Hazards.Count>0,"Pattern spawns: "+pattern+" stage "+stage+" safe "+safe);
                foreach(var h in g.Hazards)
                {
                    ok(h.occupiedLanes>=1&&h.occupiedLanes<=2,"One/two lane width");
                    if(Mathf.Abs(h.velocity.x)<.01f)
                    {
                        float growth=h.kind==FastingTopDownHazard.Kind.Quicksand?h.quicksandMaximumScale:1.25f;
                        ok(Mathf.Abs(h.transform.position.x-(safe-2)*g.settings.laneSpacing)>h.size.x*.5f*growth+.28f,"Maximum size preserves escape corridor: "+pattern+" "+h.kind);
                        ok(h.body.bounds.min.y>g.gameCamera.transform.position.y+g.gameCamera.orthographicSize,"Appears from above screen");
                    }
                }
            }
            g.Begin();var seen=new HashSet<FastingObstaclePatterns.Pattern>();for(int i=0;i<80;i++){var previous=patterns.CurrentPattern;patterns.BeginAttack();seen.Add(patterns.CurrentPattern);ok(patterns.CurrentPattern!=previous,"Avoid repeating attack pattern");}ok(seen.Count==5,"All five patterns selected");
            patterns.choices=Array.Empty<FastingObstaclePatterns.Choice>();patterns.BeginAttack();ok(patterns.CurrentPattern==FastingObstaclePatterns.Pattern.Random,"Empty inspector list safely falls back");patterns.choices=choices;
            g.Begin();Quiet(g);for(int i=0;i<8;i++)g.Tick(.1f,Vector2.zero,true);ok(g.DirectPrayerSeconds>.4f&&g.DirectPrayerSeconds<.6f,"Only active prayer seconds counted");float seconds=g.DirectPrayerSeconds;
            var book=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book);g.Collect(book);ok(g.Prayer==g.PrayerTarget&&g.DirectPrayerSeconds==seconds,"Scripture reward unchanged; no fake prayer time");
            g.Begin();Quiet(g);var hAvoid=UnityEngine.Object.Instantiate(g.hazardPrefab,g.hazardRoot);hAvoid.kind=FastingTopDownHazard.Kind.Rock;hAvoid.warning=0;hAvoid.velocity=Vector2.zero;hAvoid.ConfigureLanes(1,3);hAvoid.transform.position=new Vector3(0,-50);((List<FastingTopDownHazard>)g.Hazards).Add(hAvoid);g.Tick(.01f,Vector2.zero);ok(g.AvoidedHazards==1,"Passed obstacle counted");
            g.Begin();g.TryDamage("Sand");int hits=g.DamageHitCount;g.TryDamage("Sand");ok(hits==1&&g.DamageHitCount==1,"Invulnerable contacts not counted");g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));Field(g,"grace",0f);g.TryDamage("Rock");ok(g.DamageHitCount==1,"Shield block not counted as health hit");
            PlayerPrefs.SetInt(keys[1],1);g.Begin();Quiet(g);Set(g,"Prayer",g.PrayerTarget*.5f);for(int i=0;i<8;i++)g.Tick(.1f,Vector2.zero,true);
            g.TryDamage("Sand");Field(g,"grace",0f);g.TryDamage("Cactus");Field(g,"grace",0f);g.TryDamage("Cactus");
            ok(!g.Running&&g.resultPanel.activeSelf,"Failure shows result");ok(g.DamageHitCount==3&&g.result.text.Contains("선인장 · 2회"),"Most hit obstacle report");ok(g.result.text.Contains("직접 기도")&&g.result.text.Contains("지난 도전보다"),"Prayer and progress comparison");
            g.result.ForceMeshUpdate();ok(g.result.preferredHeight<=g.result.rectTransform.rect.height,"Result text fits");
            var fx=UnityEngine.Object.FindAnyObjectByType<FastingJourneyPresentation>();fx.Refresh(.2f);ok(fx.atmosphere.color.a==0,"Effects hidden on failure");
            g.Begin();fx.Refresh(.2f);ok(!fx.transform.Find("DayAnnouncement"),"Date popup removed");Set(g,"Elapsed",17f);fx.Refresh(.1f);ok(fx.atmosphere.color.b>fx.atmosphere.color.r&&fx.atmosphere.color.a<=.181f,"Night tint bounded and blue");
            Set(g,"Prayer",g.PrayerTarget*.5f);fx.Refresh(.2f);ok(!fx.transform.Find("DayAnnouncement"),"No popup after day change");g.Begin();ok(g.DamageHitCount==0&&g.AvoidedHazards==0&&g.DirectPrayerSeconds==0,"Retry resets run statistics");
            return n+" checks passed: 5 patterns x 4 stages x 5 corridors, spawn positions, weighted selection, prayer/item rules, statistics, results and day/night.";
        }
        finally{patterns.choices=choices;for(int i=0;i<keys.Length;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();UnityEngine.Random.state=random;Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
