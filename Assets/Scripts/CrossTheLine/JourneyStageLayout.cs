using System;
using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine
{
 public sealed class JourneyStageLayout:MonoBehaviour
 {
  [Serializable] public class Anchor { public string key;public Vector3 position;[HideInInspector]public Vector3 defaultPosition; }
  public List<Anchor> anchors=new List<Anchor>();
  public float stormSurvivalSeconds=20, fishingTimingHalfWidth=.12f, fishingIndicatorSpeed=.43f;
  public int fishingPullsRequired=6;
  public static bool Capture;
  public static Vector3 Point(Component owner,Transform target,string key,Vector3 fallback)
  {
   var root=JourneyObjectFactory.ActiveStage;if(!root)return fallback;
   var layout=root.GetComponent<JourneyStageLayout>();if(!layout)return fallback;
   string id=(target?target.name:"Scene")+" / "+key;
   var anchor=layout.anchors.Find(a=>a.key==id);
   if(anchor!=null)return (anchor.defaultPosition-fallback).sqrMagnitude<.00001f?anchor.position:fallback;
   if(Capture)layout.anchors.Add(new Anchor{key=id,position=fallback,defaultPosition=fallback});
   return fallback;
  }
  public static JourneyStageLayout Current => JourneyObjectFactory.ActiveStage?JourneyObjectFactory.ActiveStage.GetComponent<JourneyStageLayout>():null;
 }
}
