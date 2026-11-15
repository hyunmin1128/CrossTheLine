using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestShieldOrbitOutsideMembrane
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play minigame");g.enabled=false;int positions=0;
        try
        {
            var pickup=g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield);
            for(int count=1;count<=3;count++)foreach(bool acquiring in new[]{true,false})
            {
                g.Begin();for(int n=0;n<count;n++)g.Collect(pickup);if(!acquiring)g.playerAura.Advance(1);
                var orbit=g.GetComponent<FastingShieldOrbit>();var field=typeof(FastingShieldOrbit).GetField("angle",BindingFlags.NonPublic|BindingFlags.Instance);
                for(int step=0;step<72;step++)
                {
                    field.SetValue(orbit,step*Mathf.PI*2/72);orbit.Refresh();
                    Vector2 center=g.playerBody.bounds.center;Vector3 scale=g.playerAura.transform.lossyScale;Vector2 r=Vector2.Scale(g.playerAura.MembraneRadii,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));
                    var renderers=g.player.Find("OrbitingShields").GetComponentsInChildren<SpriteRenderer>();
                    if(renderers.Length!=count)throw new Exception("Shield count changed");
                    foreach(var shield in renderers)
                    {
                        Vector2 d=(Vector2)shield.bounds.center-center,e=shield.bounds.extents;
                        float x=Mathf.Max(0,Mathf.Abs(d.x)-e.x)/r.x,y=Mathf.Max(0,Mathf.Abs(d.y)-e.y)/r.y;
                        if(x*x+y*y<=1)throw new Exception("Shield rectangle overlaps membrane at step "+step);
                        positions++;
                    }
                }
            }
            return positions+" shield positions outside membrane: full 360 degrees at 5-degree intervals, 1/2/3 shields, acquisition expansion and steady state.";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
