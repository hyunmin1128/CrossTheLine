if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var report=new System.Text.StringBuilder();
foreach(var name in new[]{"NPC01","Yohan"}){
 var root=UnityEngine.GameObject.Find(name);var sr=root.GetComponentInChildren<UnityEngine.SpriteRenderer>();var animator=sr.GetComponent<UnityEngine.Animator>();
 float expectedX=root.transform.position.x;float expectedY=name=="NPC01" ? -3.28725f : root.transform.position.y+sr.GetComponent<SpriteFootAnchor>().localFootY;
 foreach(float time in new[]{0f,0.26f,0.51f,0.76f}){
  animator.Play(name=="NPC01"?"NPCIdle":"YohanIdle",0,time);animator.Update(0);
  if(System.Math.Abs(sr.bounds.min.y-expectedY)>0.001f || System.Math.Abs(sr.bounds.center.x-expectedX)>0.001f)throw new System.Exception(name+" frame misaligned: "+sr.sprite.name+" "+sr.bounds);
  report.AppendLine(name+" "+sr.sprite.name+" feet="+sr.bounds.min.y+" centerX="+sr.bounds.center.x);
 }
 if(name=="NPC01" && (root.GetComponent<UnityEngine.SpriteRenderer>() || root.GetComponent<UnityEngine.Animator>()))throw new System.Exception("Root still owns visual components");
}
return report.ToString();
