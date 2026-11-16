using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CrossTheLine.Minigames;
public static class TestFastingBoundaryMovement
{
    static void Quiet(FastingTopDownGame g)
    {
        foreach(var e in g.entities)e.gameObject.SetActive(false);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(FastingEncounterDirector).GetField("nextWave",flags).SetValue(g.encounterDirector,100f);
        typeof(FastingEncounterDirector).GetField("remaining",flags).SetValue(g.encounterDirector,100f);
    }
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g.playArea)throw new Exception("Play minigame required");g.enabled=false;int n=0;
        Action<bool,string> ok=(b,s)=>{if(!b)throw new Exception(s);n++;};
        try
        {
            for(int stage=0;stage<4;stage++)
            {
                g.Begin();typeof(FastingTopDownGame).GetProperty("StageIndex").SetValue(g,stage);Quiet(g);
                foreach(float yEdge in new[]{0f,.5f,1f})foreach(var direction in new[]{Vector2.left,Vector2.right,new Vector2(-1,-1),new Vector2(1,-1)})
                {
                    var r=g.playArea.RootArea;g.player.position=new Vector3(r.center.x,Mathf.Lerp(r.yMin,r.yMax,yEdge));float before=g.player.position.x;
                    g.Tick(.02f,direction);
                    ok((g.player.position.x-before)*Mathf.Sign(direction.x)>.001f,"Horizontal motion blocked at stage="+stage+" yEdge="+yEdge+" input="+direction+" before="+before+" after="+g.player.position.x+" root="+g.playArea.RootArea);
                    ok(g.playArea.Contains(g.player.position),"Player remains inside viewport");
                }
                foreach(float xEdge in new[]{0f,1f})
                {
                    var r=g.playArea.RootArea;g.player.position=new Vector3(Mathf.Lerp(r.xMin,r.xMax,xEdge),r.yMin);float y=g.player.position.y;
                    g.Tick(.02f,Vector2.up);ok(g.player.position.y>y,"Move up from bottom corner");ok(g.playArea.Contains(g.player.position),"Corner stays in area");
                }
                g.player.position=g.playArea.Clamp(new Vector2(0,-100));
                for(int i=0;i<100;i++){float before=g.player.position.x;float sign=i%2==0?1:-1;g.Tick(.016f,new Vector2(sign,-1));ok((g.player.position.x-before)*sign>.001f,"Continuous bottom movement "+stage+" frame "+i);ok(g.playArea.Contains(g.player.position),"Continuous movement in area");}
            }
            return n+" checks passed across four stage speeds: bottom/top/center, diagonals, corners and 400 scrolling bottom frames.";
        }
        finally{g.Begin();g.enabled=true;Time.timeScale=1;}
    }
    public static async Task<string> Keyboard()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play required");
        var prior=UnityEngine.InputSystem.Keyboard.current;var background=InputSystem.settings.backgroundBehavior;var routing=InputSystem.settings.editorInputBehaviorInPlayMode;
        UnityEngine.InputSystem.Keyboard keyboard=null;Key[] held=Array.Empty<Key>();
        Action queue=()=>{if(keyboard!=null)InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));};
        Action current=()=>{if(keyboard!=null)keyboard.MakeCurrent();};
        try
        {
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();InputSystem.onBeforeUpdate+=queue;InputSystem.onAfterUpdate+=current;keyboard.MakeCurrent();
            g.Begin();Quiet(g);g.player.position=g.playArea.Clamp(new Vector2(0,-100));float before=g.player.position.x;
            held=new[]{Key.D,Key.S};await Task.Delay(350);
            if(g.player.position.x<=before+.1f||!g.playArea.Contains(g.player.position))throw new Exception("Actual D+S failed: before="+before+" position="+g.player.position+" area="+g.playArea.RootArea+" contains="+g.playArea.Contains(g.player.position)+" enabled="+g.enabled+" running="+g.Running+" dt="+Time.deltaTime);
            before=g.player.position.x;held=new[]{Key.A,Key.S};await Task.Delay(350);
            if(g.player.position.x>=before-.1f||!g.playArea.Contains(g.player.position))throw new Exception("Actual A+S failed: before="+before+" position="+g.player.position+" area="+g.playArea.RootArea+" contains="+g.playArea.Contains(g.player.position));
            return "Actual keyboard D+S and A+S move freely along the scrolling bottom edge.";
        }
        finally{held=Array.Empty<Key>();InputSystem.onBeforeUpdate-=queue;InputSystem.onAfterUpdate-=current;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(prior!=null)prior.MakeCurrent();InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=routing;g.Begin();g.enabled=true;Time.timeScale=1;}
    }
}
