using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestFastingDarknessWalls
{
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;var flags=BindingFlags.Instance|BindingFlags.NonPublic;var random=UnityEngine.Random.state;int checks=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);checks++;};
        try{
            for(int stage=0;stage<4;stage++){
                g.BeginStageForTesting(stage);typeof(FastingTopDownGame).GetField("vision",flags).SetValue(g,1f);g.Refresh();ok(g.caveVisionMask.Opacity==(stage==0?0:1),"Darkness stage"+stage);ok(g.visionPanels.All(p=>p.color.a==0),"No duplicate partial overlay");typeof(FastingTopDownGame).GetField("vision",flags).SetValue(g,0f);g.Refresh();ok(g.caveVisionMask.Opacity==0,"Clear on expiry");
            }
            UnityEngine.Random.InitState(191119);bool verticalSeen=false,horizontalSeen=false;
            for(int sample=0;sample<60;sample++){
                g.BeginStageForTesting(2);foreach(var e in g.entities)e.gameObject.SetActive(false);g.SpawnPatternObstacle(FastingTopDownHazard.Kind.Wall,0,1,0);ok(g.Hazards.Count>=2,"Wall group exists");var first=g.Hazards[0];bool vertical=first.verticalWall;verticalSeen|=vertical;horizontalSeen|=!vertical;
                var ordered=g.Hazards.OrderBy(h=>vertical?h.transform.position.y:h.transform.position.x).ToArray();
                for(int i=0;i<ordered.Length;i++){
                    var h=ordered[i];h.UpdateApproach(g.player.position);ok(Vector2.Distance(h.body.bounds.center,h.transform.position)<.002f,"Artwork center matches collider");ok(Vector2.Distance(h.body.bounds.size,h.size)<.002f,"Image and collision sizes match");ok(g.encounterDirector.LeavesLaneOpen(h),"Escape kept");
                    if(i>0){var prev=ordered[i-1];float end=vertical?prev.body.bounds.max.y:prev.body.bounds.max.x;float start=vertical?h.body.bounds.min.y:h.body.bounds.min.x;ok(start-end<=.002f&&start-end>=-.2f,"Segments meet without gaps");}
                }
                var wall=ordered[0];Vector2 from=(Vector2)wall.transform.position+(vertical?Vector2.left:Vector2.down)*(vertical?wall.size.x*.5f+.35f:wall.size.y*.5f+.3f);
                g.player.position=wall.transform.position;typeof(FastingTopDownGame).GetMethod("BlockRuinsWalls",flags).Invoke(g,new object[]{from});ok(!new Rect((Vector2)wall.transform.position-wall.size*.5f,wall.size).Contains(g.player.position),"Wall blocks crossing");
            }
            ok(verticalSeen&&horizontalSeen,"Both orientations generated");return checks+" checks passed: stage1 excluded, stage2-4 opaque darkness and expiry, rotated wall artwork, matching hit bounds, joined segments, escape and blocking.";
        }finally{UnityEngine.Random.state=random;Time.timeScale=1;g.BeginStageForTesting(0);g.enabled=true;}
    }
}
