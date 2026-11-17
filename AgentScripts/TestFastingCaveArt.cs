using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class TestFastingCaveArt
{
    static void SetStage(FastingTopDownGame g,int stage){typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,stage);typeof(FastingTopDownGame).GetMethod("ApplyStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{false});foreach(var e in g.entities)e.gameObject.SetActive(false);}
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g.caveArt)throw new Exception("Play cave-enabled scene");g.enabled=false;int n=0;Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);n++;};
        try
        {
            g.Begin();var original=g.backgrounds.Select(b=>b.sprite).ToArray();var sizes=g.backgrounds.Select(b=>b.bounds.size).ToArray();var art=g.caveArt;
            SetStage(g,1);ok(g.backgrounds.All(b=>b.sprite==art.background&&b.color==Color.white),"All tiles use untinted cave art");
            for(int i=0;i<g.backgrounds.Length;i++)ok(Vector3.Distance(sizes[i],g.backgrounds[i].bounds.size)<.001f,"Background dimensions preserved");
            ok(art.background.texture.width==320&&art.background.texture.height==180,"Authored 320x180 background");ok(art.batFrames.Length==10,"All ten authored bat frames");
            foreach(var kind in new[]{FastingTopDownHazard.Kind.Rock,FastingTopDownHazard.Kind.Collapse,FastingTopDownHazard.Kind.Crack,FastingTopDownHazard.Kind.Bat})
            {
                SetStage(g,1);typeof(FastingEncounterDirector).GetProperty("OpenLane").SetValue(g.encounterDirector,2);g.SpawnPatternObstacle(kind,0,1,0);var h=g.Hazards.Last();
                ok(h.body.sprite==art.PrefabFor(kind,null).body.sprite,"Authored artwork applied for "+kind);ok(h.body.color==Color.white,"No legacy purple tint "+kind);
                ok(h.body.bounds.min.y>g.gameCamera.transform.position.y+g.gameCamera.orthographicSize,"Spawn offscreen "+kind);ok(h.artworkWidth>0&&h.collisionHeight>0,"Visible artwork measurements "+kind);
                var before=h.transform.position;var scale=h.body.transform.localScale;h.UpdateApproach(g.player.position);var p=h.transform.position;h.Advance(.15f,p+Vector3.down*20,p+Vector3.down*20,out _);
                if(kind==FastingTopDownHazard.Kind.Rock){ok(h.transform.position==before&&h.scrollOnly,"Stalagmite fixed in world");ok(h.body.transform.localRotation==Quaternion.identity,"Stalagmite does not roll");}
                if(kind==FastingTopDownHazard.Kind.Collapse)ok(h.transform.position==before&&h.scrollOnly,"Cave fallen rock is fixed");
                if(kind==FastingTopDownHazard.Kind.Bat){ok(h.body.sprite!=art.batFrames[0],"Bat animation advances");ok(h.transform.position.y<before.y,"Bat approaches player");}
                if(kind==FastingTopDownHazard.Kind.Crack){h.Advance(1.1f,p+Vector3.down*20,p+Vector3.down*20,out _);ok(!h.body.enabled,"Crack danger interval hides");h.Advance(1f,p+Vector3.down*20,p+Vector3.down*20,out _);ok(h.body.enabled,"Crack returns");}
                Vector2 alphaCenter=h.body.transform.TransformPoint(h.artworkCenter);ok(Mathf.Abs(alphaCenter.x-h.transform.position.x)<.001f&&Mathf.Abs(alphaCenter.y-h.transform.position.y)<.001f,"Artwork centered on collision "+kind);
            }
            SetStage(g,2);for(int i=0;i<g.backgrounds.Length;i++)ok(g.backgrounds[i].sprite==original[i]&&Vector3.Distance(g.backgrounds[i].bounds.size,sizes[i])<.001f,"Later stages restore original background");
            g.Begin();for(int i=0;i<g.backgrounds.Length;i++)ok(g.backgrounds[i].sprite==original[i]&&g.backgrounds[i].color==Color.white,"Retry restores desert");
            ok(Mathf.Abs(g.gameCamera.orthographicSize-8.4375f)<.001f,"Camera unchanged");ok(g.playArea.Contains(g.playArea.Clamp(new Vector2(-100,-100))),"Play area still clamps");
            foreach(var sprite in new[]{art.background,art.stalagmite,art.crack,art.fallingRock,art.batFrames[0]}){var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));ok(importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Pixel import "+sprite.name);}
            return n+" checks passed: cave background, tile size, four prefabs, ten-frame animation, centered collisions, movement/return to desert and imports.";
        }
        finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
