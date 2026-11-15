using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CrossTheLine.Minigames;
public static class TestShieldHorizontalBelt
{
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying)throw new Exception("Play scene");g.enabled=false;int checks=0;
        try
        {
            g.Begin();g.Collect(g.entities.First(e=>e.kind==FastingTopDownEntity.Kind.Shield));g.playerAura.Advance(1);
            var orbit=g.GetComponent<FastingShieldOrbit>();var angle=typeof(FastingShieldOrbit).GetField("angle",BindingFlags.NonPublic|BindingFlags.Instance);var sprite=g.player.Find("OrbitingShields/OrbitShield1").GetComponent<SpriteRenderer>();var aura=g.playerAura.GetComponent<MeshRenderer>();
            float maxX=0,maxY=0;
            for(int step=0;step<72;step++)
            {
                float phase=step*Mathf.PI*2/72;angle.SetValue(orbit,phase);orbit.Refresh();Vector2 offset=sprite.bounds.center-g.playerBody.bounds.center;maxX=Mathf.Max(maxX,Mathf.Abs(offset.x));maxY=Mathf.Max(maxY,Mathf.Abs(offset.y));
                bool rear=Mathf.Sin(phase)>0;if(rear?sprite.sortingOrder>=g.playerBody.sortingOrder:sprite.sortingOrder<=aura.sortingOrder)throw new Exception("Front/back ordering wrong");checks++;
            }
            if(maxX<maxY*2)throw new Exception("Path is not horizontal");checks++;
            angle.SetValue(orbit,Mathf.PI*1.5f);orbit.Refresh();if(sprite.color!=Color.white)throw new Exception("Front shield tinted");checks++;
            return checks+" checks passed: horizontal belt, rear occlusion, front above membrane.";
        }
        finally{g.Begin();g.enabled=true;}
    }
}
