using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class TestDesertObstacleSprites
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g||!g.desertArt)throw new Exception("Play scene with desert artwork");int count=0;var report=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;report.AppendLine("PASS: "+s);};var previews=new System.Collections.Generic.List<GameObject>();
        try{
            g.enabled=false;g.Begin();var kinds=new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Sand,FastingTopDownHazard.Kind.Cactus,FastingTopDownHazard.Kind.Quicksand};
            foreach(var kind in kinds){var sprite=g.desertArt.For(kind);check(sprite&&sprite.rect.width==64&&sprite.rect.height==64,"Exact 64x64 sprite: "+kind);check(sprite.texture.filterMode==FilterMode.Point&&sprite.pixelsPerUnit==32,"Point filter and 32 PPU: "+kind);var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));var colors=tex.GetPixels32();check(colors.Distinct().Count()<=16,"Maximum sixteen RGBA colors: "+kind);check(colors.Any(c=>c.a==0)&&colors.All(c=>c.a==0||c.a==255),"Hard transparent background, no soft alpha: "+kind);check(colors.Any(c=>c.a==255&&c.r==0&&c.g==0&&c.b==0),"Black outline palette present: "+kind);UnityEngine.Object.Destroy(tex);check(g.desertArt.PrefabFor(kind,null)!=null,"Editable prefab exists: "+kind);}
            for(int n=0;n<4;n++)g.SpawnWave();check(g.Hazards.Count==4,"First four desert waves spawn four hazards");foreach(var h in g.Hazards){check(h.body.sprite==g.desertArt.For(h.kind)&&h.body.color==Color.white,"Actual spawned hazard uses art: "+h.kind);check(h.size==new Vector2(1.7f,1),"Collision size unchanged: "+h.kind);var size=h.size;h.UpdateApproach(g.player.position);check(h.size==size,"Approach enlargement preserves hit size: "+h.kind);}
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);for(int i=0;i<kinds.Length;i++){var h=UnityEngine.Object.Instantiate(g.desertArt.PrefabFor(kinds[i],g.hazardPrefab),g.hazardRoot);previews.Add(h.gameObject);h.kind=kinds[i];g.desertArt.Apply(h);h.transform.position=new Vector3(-4.5f+i*3,5);h.velocity=Vector2.zero;if(h.kind==FastingTopDownHazard.Kind.Sand)h.arrow.transform.localRotation=Quaternion.Euler(0,0,-90);h.warning=0;h.Advance(.01f,new Vector2(100,100),new Vector2(100,100),out _);h.UpdateApproach(g.player.position);check(h.body.enabled,"Runtime sprite visible: "+h.kind);}
            Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/DesertObstacleSprites.png"));await Task.Delay(300);File.WriteAllText("Docs/QA/FastingTopDown/DesertObstacleSpritesValidation.txt",report.ToString());return count+" checks passed";
        }finally{foreach(var o in previews)if(o)UnityEngine.Object.Destroy(o);g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
