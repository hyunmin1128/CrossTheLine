using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class TestUpdatedTopDownBackgroundNov14
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");var director=g.encounterDirector;g.encounterDirector=null;g.enabled=false;int checks=0;
        Action<bool,string> ok=(v,m)=>{if(!v)throw new Exception(m);checks++;};
        try
        {
            g.Begin();typeof(FastingTopDownGame).GetField("wave",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,float.MaxValue);
            ok(g.backgrounds.All(b=>AssetDatabase.GetAssetPath(b.sprite)=="Assets/Sprite/Background/Chapter3-Minigame-Stage1.png"),"Updated art on all four tiles");
            ok(g.backgrounds[0].sprite.texture.width==320&&g.backgrounds[0].sprite.texture.height==180,"320x180 source");
            ok(Mathf.Abs(g.backgrounds[0].bounds.size.y-g.gameCamera.orthographicSize*2)<.001f,"Background fits camera height");
            float height=g.backgrounds[0].bounds.size.y;
            for(int i=0;i<2500;i++)
            {
                foreach(var e in g.entities)e.gameObject.SetActive(false);g.Tick(.05f,new Vector2(i%200<100?.1f:-.1f,0));
                if(i%100!=0)continue;
                var tiles=g.backgrounds.OrderBy(b=>b.bounds.center.y).ToArray();
                ok(Enumerable.Range(1,tiles.Length-1).All(n=>Mathf.Abs(tiles[n].bounds.min.y-tiles[n-1].bounds.max.y)<.001f),"No gap after scrolling "+i);
                ok(tiles[0].bounds.min.y<=g.gameCamera.transform.position.y-g.gameCamera.orthographicSize&&tiles.Last().bounds.max.y>=g.gameCamera.transform.position.y+g.gameCamera.orthographicSize,"Camera covered after scrolling "+i);
            }
            ok(g.player.position.y>300,"Long scrolling actually advanced");
            return checks+" checks passed: updated references, source size, camera fit and 125 seconds simulated seamless geometry.";
        }
        finally{g.encounterDirector=director;g.Begin();g.enabled=true;}
    }
}
