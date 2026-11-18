using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class TestFastingReadabilityHearts
{
    public static string Run(){
        if(!Application.isPlaying)throw new Exception("Play required");
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();g.enabled=false;int n=0;Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        try{
            g.Begin();ok(g.fullHeart&&g.halfHeart&&g.emptyHeart,"All heart assets connected");
            ok(AssetDatabase.GetAssetPath(g.fullHeart).EndsWith("Heart.aseprite"),"Native updated full heart");
            typeof(FastingTopDownGame).GetProperty("Health").SetValue(g,3.5f);g.RefreshHealthHearts();
            for(int i=0;i<5;i++)ok(g.hearts[i].sprite==(i<3?g.fullHeart:i==3?g.halfHeart:g.emptyHeart),"Full/half/empty at "+i);
            var pp=g.gameCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            ok(pp.refResolutionX==960&&pp.refResolutionY==540,"960x540 reference preserved");
            ok(pp.cropFrame==UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.StretchFill,"Aspect-fit fill enabled");
            ok(Mathf.Abs(g.gameCamera.orthographicSize-8.4375f)<.001f,"World scale preserved");
            var aura=UnityEngine.Object.FindAnyObjectByType<FastingPlayerAura>();aura.Advance(.01f);ok(!aura.PrayerGlowVisible,"No idle prayer bubble");
            for(int i=0;i<4;i++)g.Tick(.1f,Vector2.zero,true);aura.Advance(.01f);ok(aura.PrayerGlowVisible,"Bubble appears on actual prayer");
            var mesh=aura.GetComponent<MeshFilter>().sharedMesh;ok(mesh.vertexCount>100&&mesh.bounds.size.x>2&&mesh.bounds.size.y>2.8f,"Large surrounding bubble geometry");
            g.Tick(.01f,Vector2.zero,false);aura.Advance(.01f);ok(!aura.PrayerGlowVisible,"Bubble stops on release");
            typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,1);
            typeof(FastingTopDownGame).GetField("vision",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,1f);g.Refresh();
            ok(g.caveVisionMask.Opacity==1&&g.visionPanels.All(p=>p.color.a==0),"Cave fully obscured outside bubble");
            using(var vh=new UnityEngine.UI.VertexHelper()){
                typeof(FastingVisionMask).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Invoke(g.caveVisionMask,new object[]{vh});
                var v=new UnityEngine.UIVertex();vh.PopulateUIVertex(ref v,0);ok(v.color.a==255,"Outside mask is opaque black");
                bool clear=false;for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref v,i);if(v.color.a==0){clear=true;break;}}ok(clear,"Character surroundings remain visible");
            }
            typeof(FastingTopDownGame).GetField("vision",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,0f);g.Refresh();ok(g.caveVisionMask.Opacity==0,"Vision restores after effect");
            return n+" checks passed: native full/half/empty hearts, camera scale/fill, prayer-only bubble geometry, opaque cave mask and restoration.";
        }finally{Time.timeScale=1;g.Begin();g.enabled=true;}
    }
}
