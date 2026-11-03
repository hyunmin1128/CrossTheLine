using UnityEngine;
namespace CrossTheLine
{
 public sealed partial class DesertTemptation
 {
  Vector3 ascentStart; int ascentDestination=2;
  float RouteY(float x)
  {
   var ridge=stageObjects.transform.Find("SummitRidgeFootPath");
   if(ridge&&ridge.childCount>1&&x>=ridge.GetChild(0).position.x&&x<=ridge.GetChild(ridge.childCount-1).position.x)
   {
    for(int j=1;j<ridge.childCount;j++){var a=ridge.GetChild(j-1).position;var b=ridge.GetChild(j).position;if(x<=b.x)return Mathf.Lerp(a.y,b.y,Mathf.InverseLerp(a.x,b.x,x));}
   }
   int i=0;for(int j=1;j<locations.Length;j++)if(x>=locations[j].entrance.position.x)i=j;
   return locations[i].GroundAt(x);
  }
  void BeginHillAscent(bool resume)
  {
   ascentDestination=TrialIndex+1;var hill=locations[ascentDestination];
   if(resume)jesus.position=locations[ascentDestination-1].jesusMark.position;
   ascentStart=jesus.position;CurrentStage=Stage.Ascent;clock=0;asked=false;
   film.SetActive(false);game.BaptismCinema(false);game.BaptismWalk(false,1);
   staff.gameObject.SetActive(false);satan.gameObject.SetActive(false);
   SetScrollHud(ascentDestination==2?"두 번째 시험 · 언덕의 능선으로":"세 번째 시험 · 산꼭대기의 공터로","A / D · ← / →  길을 따라 다음 시험 장소로 이동하세요");
  }
  float HillY(float x)=>RouteY(x);
  void TickHillAscent(float dt)
  {
   float input=HorizontalInput();var p=jesus.position;
   p.x=Mathf.Clamp(p.x+input*scrolling.speed*.7f*dt,ascentStart.x,locations[ascentDestination].jesusMark.position.x);
   p.y=RouteY(p.x);p.z=0;jesus.position=p;game.BaptismWalk(input!=0,input);
   location=0;for(int j=1;j<locations.Length;j++)if(p.x>=locations[j].entrance.position.x)location=j;
   if(p.x>=locations[ascentDestination].jesusMark.position.x-.03f){location=ascentDestination;RevealTrial();}
  }
 }
}
