using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class VerifyReaperTracking
{
 const string Report="Docs/QA/ReaperTracking-20261120";
 static FastingTopDownHazard Spawn(FastingTopDownGame game)
 {
  for(int i=0;i<20;i++){game.SpawnPatternObstacle(FastingTopDownHazard.Kind.Reaper,0,4,0);var h=game.Hazards.FirstOrDefault(x=>x&&x.kind==FastingTopDownHazard.Kind.Reaper);if(h)return h;}
  throw new Exception("Actual stage formation did not spawn");
 }
 public static async Task<string> Verify()
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
  int frame=Time.frameCount;while(Time.frameCount<frame+4)await Task.Yield();
  var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var errors=new List<string>();var rows=new List<string>();
  Application.LogCallback capture=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=capture;
  Action<bool,string> check=(ok,m)=>{if(!ok)throw new Exception(m);rows.Add("PASS "+m);};
  Directory.CreateDirectory(Report);
  try{
   game.BeginStageForTesting(3);game.enabled=false;
   var h=Spawn(game);var sweep=h.GetComponent<FastingReaperSweep>();
   check(sweep.Choreographed,"Actual final-stage spawn starts choreography");check(h.body.enabled&&h.body.sprite==sweep.slashFrames[0],"First pose visible immediately");check(!h.Active,"Following cannot damage");
   float hit;float originalX=h.transform.position.x;
   game.player.position=new Vector3(game.SpawnRoadRange.x,game.player.position.y);h.Advance(.3f,game.player.position,game.player.position,out hit);float left=h.transform.position.x;
   game.player.position=new Vector3(game.SpawnRoadRange.y,game.player.position.y);h.Advance(.3f,game.player.position,game.player.position,out hit);
   check(h.transform.position.x>left,"Following tracks player horizontal movement");check(h.velocity==Vector2.zero&&h.body.transform.localRotation==Quaternion.identity,"Following has no attack drift or rocking");check(game.FitsSpawnRoad(h),"Tracking stays inside road");
   float leftTime=sweep.followSeconds-.6f;
   check(!h.Advance(leftTime,game.player.position,game.player.position,out hit),"Tracking-to-lock boundary cannot deal damage");
   check(sweep.CurrentPhase==FastingReaperSweep.AttackPhase.Preparing&&h.body.sprite==sweep.slashFrames[1],"Second pose begins locked preparation");check(sweep.RangeVisible&&!h.Active,"Visible attack range while harmless");
   var locked=h.transform.position;game.player.position=new Vector3(game.SpawnRoadRange.x,game.player.position.y);
   check(!h.Advance(.4f,game.player.position,game.player.position,out hit),"Preparation remains harmless");check(h.transform.position==locked,"Locked position stops following player");
   var lines=h.GetComponentsInChildren<LineRenderer>();var range=lines.Single(l=>l.name=="LockedAttackRange");check(Mathf.Abs(range.GetPosition(1).x-range.GetPosition(0).x-h.size.x)<.001f,"Telegraph width matches attack hit area");
   h.Advance(sweep.lockSeconds-.4f+.01f,new Vector2(100,100),new Vector2(100,100),out hit);
   check(sweep.CurrentPhase==FastingReaperSweep.AttackPhase.Dashing&&h.body.sprite==sweep.slashFrames[2],"Third pose starts dash");check(!sweep.RangeVisible&&h.Active,"Warning clears when attack starts");
   check(Mathf.Abs(h.transform.position.y-locked.y+sweep.dashSpeed*.01f)<.002f,"Only leftover boundary time moves dash");
   var start=h.transform.position;
   for(int i=0;i<5;i++)h.Advance(.05f,new Vector2(100,100),new Vector2(100,100),out hit);
   check(Mathf.Abs(h.transform.position.x-start.x)<.0001f&&Mathf.Abs(h.transform.position.y-start.y+sweep.dashSpeed*.25f)<.001f,"Fast dash follows fixed vertical line");check(h.body.sprite==sweep.slashFrames[2]&&h.body.transform.localRotation==Quaternion.identity,"Dash holds third pose without swinging");
   var collision=h.CollisionCenter+Vector2.down*sweep.dashSpeed*.04f;check(h.Advance(.08f,collision,collision,out hit),"Swept collision catches fast pass-through");
   var safe=new Vector2(game.SpawnLaneX(game.encounterDirector.OpenLane),h.CollisionCenter.y-1);check(!h.Advance(.05f,safe,safe,out hit),"Reserved escape lane remains outside slash");
   check(errors.Count==0,"No runtime errors during checks");File.WriteAllLines(Report+"/validation.txt",rows);return "PASS "+rows.Count+" checks on actual spawned reaper";
  }catch(Exception e){File.WriteAllLines(Report+"/validation.txt",rows.Concat(new[]{"FAIL "+e}));throw;}
  finally{Application.logMessageReceived-=capture;game.BeginStageForTesting(0);game.enabled=true;}
 }
 public static string Preview()
 {
  var game=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();game.BeginStageForTesting(3);game.enabled=false;var h=Spawn(game);float t;
  game.player.position=new Vector3(0,game.player.position.y);h.Advance(h.GetComponent<FastingReaperSweep>().followSeconds+.3f,new Vector2(100,100),new Vector2(100,100),out t);
  return "Locked preparation preview ready";
 }
}
