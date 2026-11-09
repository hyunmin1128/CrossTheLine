using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestUserFastingPickups
{
    static string Name(FastingTopDownEntity.Kind k)=>k==FastingTopDownEntity.Kind.Book?"Bible":k==FastingTopDownEntity.Kind.Page?"Page":k==FastingTopDownEntity.Kind.Shield?"ShieldOfFaith":k==FastingTopDownEntity.Kind.Fragment?"Piece":null;
    public static async Task<string> Run(){var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play scene");int count=0,items=0;var log=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;log.AppendLine("PASS: "+s);};
        try{
            g.enabled=false;g.Begin();foreach(var e in g.entities){check(e!=null,"Entity reference preserved");string n=Name(e.kind);if(n==null)continue;items++;var r=e.transform.Find("Symbol").GetComponent<SpriteRenderer>();check(AssetDatabase.GetAssetPath(r.sprite)=="Assets/Sprite/Objects/"+n+".aseprite","User source connected: "+e.name);check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/"+n+"Pickup.prefab").GetComponent<FastingTopDownEntity>().kind==e.kind,"Typed prefab kind matches: "+e.name);check(Vector2.Distance(r.bounds.center,e.transform.position)<.001f,"Sprite centered on pickup footprint: "+e.name);check(Mathf.Abs(r.transform.lossyScale.x-r.sprite.pixelsPerUnit/32f)<.001f&&r.sprite.texture.filterMode==FilterMode.Point,"Pixel scale/filter preserved: "+e.name);}
            check(items==20,"All twenty matching pickups replaced across four stages");check(g.settings.pickupChance==.2f&&g.settings.prayerPerSecond==1,"Spawn chance and prayer rate unchanged");var page=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Page);var piece=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Fragment);var bible=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Book);var shield=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield);g.Collect(page);check(Mathf.Abs(g.Prayer-g.PrayerTarget*.25f)<.001f,"Page effect preserved");g.Collect(piece);check(Mathf.Abs(g.Prayer-g.PrayerTarget*.35f)<.001f,"Piece effect preserved");g.Collect(bible);check(g.Prayer==g.PrayerTarget&&g.GateVisible,"Bible effect preserved");g.Collect(shield);float health=g.Health;g.TryDamage("test");check(!g.Shielded&&g.Health==health,"Shield effect preserved");
            g.Begin();foreach(var e in g.entities)e.gameObject.SetActive(false);var display=new[]{bible,page,shield,piece};for(int i=0;i<display.Length;i++){display[i].transform.position=new Vector3(-4.5f+i*3,4);display[i].gameObject.SetActive(true);display[i].consumed=false;}g.Refresh();Directory.CreateDirectory("Docs/QA/FastingTopDown");ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/UserPickupSprites.png"));await Task.Delay(300);File.WriteAllText("Docs/QA/FastingTopDown/UserPickupSpritesValidation.txt",log.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}}
}
