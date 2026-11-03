if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var player=UnityEngine.Object.FindFirstObjectByType<JesusController>();
var body=player.GetComponent<UnityEngine.Rigidbody2D>();
var log=new System.Text.StringBuilder();
System.Collections.IEnumerator Verify()
{
 player.SetMovementLocked(true);player.ScriptedMove=1;body.position=new UnityEngine.Vector2(-36,-1.38f);body.linearVelocity=UnityEngine.Vector2.zero;
 float start=UnityEngine.Time.time;float lowest=100;
 while(player.transform.position.x < -11 && UnityEngine.Time.time-start<12){lowest=UnityEngine.Mathf.Min(lowest,player.transform.position.y);yield return null;}
 log.AppendLine("Rightward: "+player.transform.position+" minimumY="+lowest);
 player.ScriptedMove=-1;start=UnityEngine.Time.time;
 while(player.transform.position.x > -36 && UnityEngine.Time.time-start<12)yield return null;
 log.AppendLine("Leftward: "+player.transform.position);
 player.ScriptedMove=0;player.SetMovementLocked(false);
 body.position=new UnityEngine.Vector2(-24,-3.28f);body.linearVelocity=UnityEngine.Vector2.zero;
 yield return new UnityEngine.WaitForSeconds(0.5f);
 var npc=UnityEngine.GameObject.Find("Yohan").GetComponent<HomeInteractable>();var ui=player.dialogueUI;
 npc.Interact(ui);ui.dialogueText.maxVisibleCharacters=int.MaxValue;ui.Advance();ui.dialogueText.maxVisibleCharacters=int.MaxValue;ui.Advance();
 log.AppendLine("Ceremony started="+npc.GetComponent<BaptismSequence>().IsPlaying+" suspended="+ui.IsSuspended);
 yield return new UnityEngine.WaitForSeconds(1.5f);
 log.AppendLine("Pose="+player.characterSprite.sprite.name+" YohanState="+npc.GetComponentInChildren<UnityEngine.Animator>().GetCurrentAnimatorStateInfo(0).IsName("YohanBaptism"));
 yield return new UnityEngine.WaitForSeconds(3);
 log.AppendLine("After ceremony suspended="+ui.IsSuspended+" line="+ui.dialogueText.text+" locked="+player.MovementLocked);
 for(int i=0;i<8&&ui.IsOpen;i++){ui.dialogueText.maxVisibleCharacters=int.MaxValue;ui.Advance();}
 npc.Interact(ui);log.AppendLine("Completed="+npc.DialogueCompleted+" reopened="+ui.IsOpen);

 System.IO.File.WriteAllText("Temp/main-scene-verification.txt",log.ToString());
}
player.StartCoroutine(Verify());return "Verification running";
