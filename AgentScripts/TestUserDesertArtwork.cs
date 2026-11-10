using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class TestUserDesertArtwork
{
    public static async Task<string> Run(){var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play scene");int count=0;var log=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;log.AppendLine("PASS: "+s);};
        try{g.enabled=false;g.Begin();foreach(var bg in g.backgrounds){check(AssetDatabase.GetAssetPath(bg.sprite)=="Assets/Sprite/Background/Chapter3-Minigame-Stage1.png","User background connected");check(bg.sprite.texture.width==320&&bg.sprite.texture.height==180&&bg.transform.localScale.x==3&&bg.transform.localScale.y==3,"320x180 artwork displayed at exact 3x");check(Mathf.Abs(bg.bounds.size.x-30)<.001f&&Mathf.Abs(bg.bounds.size.y-16.875f)<.001f,"Original camera/world background size preserved");}check(g.gameCamera.orthographicSize==8.4375f,"Camera zoom unchanged");string[] files={"Rock","SandStorm","Cactus","SandHell"};var kinds=new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Quicksand};for(int i=0;i<4;i++){var s=g.desertArt.For(kinds[i]);check(AssetDatabase.GetAssetPath(s)=="Assets/Sprite/Objects/"+files[i]+".png"&&s.rect.width==64&&s.rect.height==64,"User obstacle source and 64x64 size: "+files[i]);check(g.desertArt.PrefabFor(kinds[i],null).body.sprite==s&&s.texture.filterMode==FilterMode.Point,"Prefab points at new art with pixel filter: "+files[i]);}
            foreach(var e in g.entities)e.gameObject.SetActive(false);for(int i=0;i<4;i++)g.SpawnWave();check(g.Hazards.All(h=>h.body.sprite==g.desertArt.For(h.kind)),"Actual four spawned obstacles use user art");check(g.Hazards.All(h=>h.body.bounds.min.y>g.gameCamera.transform.position.y+g.gameCamera.orthographicSize),"New artwork starts completely outside top edge");
            for(int i=0;i<g.Hazards.Count;i++){var h=g.Hazards[i];h.transform.position=new Vector3(-4.5f+i*3,6);h.velocity=Vector2.zero;h.Advance(.01f,new Vector2(100,100),new Vector2(100,100),out _);}
            Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/UserDesertArtwork.png"));await Task.Delay(300);File.WriteAllText("Docs/QA/FastingTopDown/UserDesertArtworkValidation.txt",log.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}}
}
