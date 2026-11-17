using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingCaveGroups
{
    static void Field(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
    static void Set(object o,string n,object v)=>o.GetType().GetProperty(n).SetValue(o,v);
    static void Cave(FastingTopDownGame g)
    {
        g.Begin();Set(g,"StageIndex",1);typeof(FastingTopDownGame).GetMethod("ApplyStage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{false});foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g.encounterDirector,"OpenLane",0);Field(g.encounterDirector,"nextWave",100f);Field(g.encounterDirector,"nextRandom",100f);
    }
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");g.enabled=false;int n=0;var random=UnityEngine.Random.state;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        try
        {
            UnityEngine.Random.InitState(217);
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Bat,FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Collapse})
            {
                var seen=new HashSet<int>();for(int sample=0;sample<40;sample++)
                {
                    Cave(g);g.SpawnPatternObstacle(kind,1,1,0);var members=g.Hazards.ToArray();seen.Add(members.Length);int max=kind==FastingTopDownHazard.Kind.Bat?3:2;
                    ok(members.Length>=1&&members.Length<=max,"Group size "+kind);ok(members.Select(h=>h.formationId).Distinct().Count()==1&&members[0].formationId>0,"Shared group identity");
                    ok(members.All(h=>h.occupiedLanes==1&&h.formationCount==members.Length),"Individual collision actors");
                    for(int i=0;i<members.Length;i++)
                    {
                        var h=members[i];ok(g.encounterDirector.LeavesLaneOpen(h),"Escape corridor "+kind);if(i>0)ok(Mathf.Abs(h.transform.position.x-members[i-1].transform.position.x-g.settings.laneSpacing)<.001f,"Adjacent columns");
                        var p=h.transform.position;var oldFrame=h.body.sprite;h.Advance(.16f,p+Vector3.down*100,p+Vector3.down*100,out _);
                        if(kind==FastingTopDownHazard.Kind.Bat)ok(h.velocity.y<0&&h.transform.position.y<p.y&&h.body.sprite!=oldFrame,"Animated flying bat");
                        else ok(h.velocity==Vector2.zero&&h.transform.position==p&&h.body.transform.localRotation==Quaternion.identity,"Ground obstacle stays fixed");
                        var center=h.transform.position;ok(h.Advance(.001f,center,center,out _),"Per-member collision detects center");
                    }
                }
                ok(seen.Count==(kind==FastingTopDownHazard.Kind.Bat?3:2),"All requested group sizes generated "+kind);
            }
            Cave(g);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Crack,1,1,0);var crack=g.Hazards.Single();ok(crack.velocity==Vector2.zero&&crack.expandsOnApproach,"Crack is fixed and grows");var baseSize=crack.size;crack.quicksandMaximumScale=2;crack.UpdateApproach(crack.transform.position);ok(Vector2.Distance(crack.size,baseSize*2)<.001f&&crack.ApproachScale==2,"Crack reaches 2x2 area");
            var safe=new Vector2(-2*g.settings.laneSpacing,crack.transform.position.y);ok(!crack.Advance(.01f,safe,safe,out _),"Maximum crack leaves escape lane");
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);Set(g.encounterDirector,"OpenLane",4);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Sand,0,2,0);ok(g.Hazards.Any(h=>h.kind==FastingTopDownHazard.Kind.Sand&&h.occupiedLanes==2),"Desert sand remains two-wide");
            Cave(g);var aura=g.playerAura;Set(g.encounterDirector,"CurrentPhase",FastingEncounterDirector.Phase.PrayerWindow);g.Tick(.01f,Vector2.zero,false);aura.Advance(.01f);var fx=aura.GetComponent<FastingIllustratedAura>();ok(fx&&!fx.PrayerVisible,"No particle in idle prayer window");
            g.Tick(.1f,Vector2.zero,true);aura.Advance(.01f);ok(g.PrayerPreparing&&!fx.PrayerVisible,"No particle during preparation");for(int i=0;i<4;i++)g.Tick(.1f,Vector2.zero,true);aura.Advance(.01f);ok(g.Praying&&fx.PrayerVisible,"Particle while actively praying");
            g.Tick(.01f,Vector2.zero,false);aura.Advance(.01f);ok(!fx.PrayerVisible,"Release stops particle");Set(g,"Running",false);aura.Advance(.01f);ok(!fx.PrayerVisible,"No particle outside gameplay");
            return n+" checks passed: bat 1–3, fixed cave groups 1–2, per-member collision, 2x2 crack and prayer-only particles.";
        }
        finally{UnityEngine.Random.state=random;Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
