using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingDropBalance
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play scene");int count=0;var log=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;log.AppendLine("PASS: "+s);};float[] pages={.25f,.30f,.35f,.40f},fragments={.10f,.15f,.20f,.25f};var setStage=typeof(FastingTopDownGame).GetProperty("StageIndex").GetSetMethod(true);
        try{
            g.enabled=false;check(Mathf.Abs(g.settings.prayerPerSecond-1)<.0001f,"Base prayer gain is one per second");check(Mathf.Abs(g.settings.pickupChance-.2f)<.0001f,"Drop chance is twenty percent per candidate");var page=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Page);var piece=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Fragment);
            for(int stage=0;stage<4;stage++){g.Begin();setStage.Invoke(g,new object[]{stage});foreach(var e in g.entities)e.gameObject.SetActive(false);for(int tick=0;tick<20;tick++)g.Tick(.05f,Vector2.zero,true);check(Mathf.Abs(g.Prayer-1)<.001f,"One second prayer in stage "+(stage+1));typeof(FastingTopDownGame).GetProperty("Prayer").GetSetMethod(true).Invoke(g,new object[]{0f});g.Collect(page);check(Mathf.Abs(g.Prayer/g.PrayerTarget-pages[stage])<.0001f,"Stage "+(stage+1)+" page fraction");g.Begin();setStage.Invoke(g,new object[]{stage});g.Collect(piece);check(Mathf.Abs(g.Prayer/g.PrayerTarget-fragments[stage])<.0001f,"Stage "+(stage+1)+" fragment fraction");check(g.entities.Count(e=>e.stage==stage&&e.kind!=FastingTopDownEntity.Kind.Cover&&e.kind!=FastingTopDownEntity.Kind.Water)==7,"Seven pickup candidates in stage "+(stage+1));}
            g.Begin();int seen=0,samples=100;var state=UnityEngine.Random.state;try{UnityEngine.Random.InitState(229);for(int n=0;n<samples;n++){g.Begin();seen+=g.entities.Count(e=>e.stage==0&&e.gameObject.activeSelf&&e.kind!=FastingTopDownEntity.Kind.Cover&&e.kind!=FastingTopDownEntity.Kind.Water);}}finally{UnityEngine.Random.state=state;}check(seen>80&&seen<200,"Sampled seeded drop count consistent with 20 percent: "+seen+" / 700 candidates");Directory.CreateDirectory("Docs/QA/FastingTopDown");File.WriteAllText("Docs/QA/FastingTopDown/DropBalanceValidation.txt",log.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
