using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using CrossTheLine.Minigames.Fishing;
public static class TestChapter08FishVariants
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<NetFishingGame>();Check(Application.isPlaying,"Play mode required");g.enabled=false;
        var source=g.settings;var config=UnityEngine.Object.Instantiate(source);int seed=g.randomSeed;var random=UnityEngine.Random.state;int[] counts=new int[4];
        try
        {
            Check(g.fishVariants.Length==4&&g.fishVariants.All(s=>s),"Four sprite references required");
            config.castSeconds=0;config.biteDelay=Vector2.zero;config.biteNoticeSeconds=0;config.startingProgress=.99f;config.progressPerGoodTap=1;config.catchZoneHeight=.9f;config.tapLift=.01f;config.resultSeconds=.01f;config.goalFish=100000;
            g.settings=config;g.randomSeed=481;g.Restart();g.StartGame();
            for(int trial=0;trial<400;trial++)
            {
                var before=g.CurrentFish;g.boat.position=new Vector3(-99,-99,0);Check(!g.TryCast()&&g.CurrentFish==before,"Rejected cast must not redraw fish");
                g.boat.position=g.fishingSpots[g.ActiveSpot].position;Check(g.TryCast(),"Valid cast must start");
                var chosen=g.CurrentFish;counts[g.CurrentFishIndex]++;g.Refresh();g.Refresh();Check(g.CurrentFish==chosen,"Refresh must not redraw fish");
                for(int i=0;i<5&&g.Session.State!=NetFishingSession.Phase.Fighting;i++)g.Tick(.02f,Vector2.zero,false,false);
                Check(g.fishRect.GetComponent<UnityEngine.UI.Image>().sprite==chosen,"Gauge must show selected fish");g.Tick(.02f,Vector2.zero,false,true);
                Check(g.Session.State==NetFishingSession.Phase.Result&&g.Session.LastCatch>0,"Successful catch expected");
                Check(g.resultFishImage.gameObject.activeSelf&&g.resultFishImage.sprite==chosen,"Result must show same fish");
                g.Tick(.02f,Vector2.zero,false,false);
                Check(!g.resultFishImage.gameObject.activeSelf,"Hide catch preview outside success result");
            }
            Check(counts.All(n=>n>=65&&n<=135),"Unexpected distribution");
        }
        finally{g.settings=source;g.randomSeed=seed;UnityEngine.Random.state=random;g.Restart();g.enabled=false;UnityEngine.Object.Destroy(config);}
        var result=new{casts=400,counts,probabilityPerType=.25,stableDuringFight=true,resultMatchesGauge=true,rejectedCastDoesNotRedraw=true,gameplaySettingsRestored=true};
        string json=JsonConvert.SerializeObject(result,Formatting.Indented);File.WriteAllText("Docs/Chapter08/FishVariants20261124/tests.json",json);return json;
    }
}
