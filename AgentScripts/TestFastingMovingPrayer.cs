using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingMovingPrayer
{
    public static async Task<string> Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||!Application.isPlaying)throw new Exception("Play top-down scene");int count=0;var report=new System.Text.StringBuilder();Action<bool,string> check=(ok,s)=>{if(!ok)throw new Exception(s);count++;report.AppendLine("PASS: "+s);};
        try{
            g.enabled=false;g.Begin();g.Tick(.1f,Vector2.zero,true);check(g.Praying&&g.Prayer>0&&g.player.position.y>0,"Prayer and automatic forward movement occur together");float prayer=g.Prayer;g.Tick(.1f,Vector2.right,true);check(g.player.position.x==3&&g.Prayer>prayer&&g.Praying,"One-lane dodge does not interrupt prayer");g.Tick(.1f,Vector2.left,true);check(g.player.position.x==0&&g.Praying,"Left dodge also supports ongoing prayer");
            var covers=g.entities.Where(e=>e.kind==FastingTopDownEntity.Kind.Cover).ToArray();foreach(var e in covers){check(Mathf.Abs(e.ShelterArea.width-g.settings.laneSpacing)<.001f&&Mathf.Abs(e.ShelterArea.height-g.settings.laneSpacing)<.001f,"Shelter cell equals lane size: "+e.name);check(Vector2.Distance(e.marker.bounds.size,e.ShelterArea.size)<.001f&&Vector2.Distance(e.marker.bounds.center,e.ShelterArea.center)<.001f,"Visible lower cell matches actual protection: "+e.name);if(e.WindCellInsideMap)check(Vector2.Distance(e.windMarker.bounds.size,e.WindShelterArea.size)<.001f&&Vector2.Distance(e.windMarker.bounds.center,e.WindShelterArea.center)<.001f,"Visible wind cell matches protection: "+e.name);else check(!e.windMarker.gameObject.activeSelf,"Out-of-map wind cell hidden: "+e.name);}
            var visible=covers.First(e=>e.stage==0&&e.WindCellInsideMap);g.player.position=visible.WindShelterArea.center;g.Tick(.01f,Vector2.zero,true);check(g.WindSheltered&&g.Praying&&visible.windMarker.color.g>.9f,"Prayer continues inside green wind-protection cell");
            g.player.position=visible.ShelterArea.center;g.Tick(.01f,Vector2.zero,true);check(g.Sheltered&&visible.marker.color.g>.9f,"Lower cell turns green only when player is protected");check(!visible.Protects(visible.ShelterArea.center+Vector2.right*1.6f),"Adjacent lane outside cell is not falsely protected");
            Directory.CreateDirectory("Docs/QA/FastingTopDown");g.Refresh();ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/QA/FastingTopDown/MovingPrayer-ShelterCells.png"));await Task.Delay(300);File.WriteAllText("Docs/QA/FastingTopDown/MovingPrayerValidation.txt",report.ToString());return count+" checks passed";
        }finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
