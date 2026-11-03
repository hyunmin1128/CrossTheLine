using UnityEditor;using UnityEngine;using CrossTheLine;
public sealed class JourneyJoinSaveGuard:AssetModificationProcessor
{
 static string[] OnWillSaveAssets(string[] paths)
 {
  if(!EditorApplication.isPlaying)foreach(var join in Object.FindObjectsByType<JourneySceneryJoin>())join.SuspendMaterial();
  return paths;
 }
}
