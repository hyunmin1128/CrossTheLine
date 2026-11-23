using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
using Newtonsoft.Json;
using CrossTheLine.Cana;
public static class ExportGameBalanceSnapshot
{
    const string Folder="Docs/Balance/2026-11-23";
    [Serializable] public sealed class Row {public string scene,objectPath,component,property,value,type;}
    [Serializable] public sealed class Rows {public List<Row> rows=new List<Row>();}
    [Serializable] public sealed class Stage {public string source,profile,advancedSource,advanced;}
    [Serializable] public sealed class Snapshot {public string scene,gameSettings,spawnSettings,desert,cave,ruins,trial,hit,vision,effects,reaper,director,patterns,aura,playArea;public Stage[] stages;}
    static string J(UnityEngine.Object value)=>value?JsonUtility.ToJson(value):"null";
    public static string Fasting()
    {
        Directory.CreateDirectory(Folder);var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        if(!g||EditorApplication.isPlaying)throw new Exception("Inspect fasting scene in edit mode");
        var result=new Snapshot{scene=g.gameObject.scene.path,gameSettings=J(g.settings),spawnSettings=J(g.spawnSettings),desert=J(g.desertArt),cave=J(g.caveArt),ruins=J(g.ruinsArt),trial=J(g.trialArt),hit=J(g.hitFeedback),vision=J(g.caveVisionMask),effects=J(g.effectSprites),reaper=J(g.trialArt.reaper.GetComponent<FastingReaperSweep>()),director=J(g.encounterDirector),patterns=J(g.obstaclePatterns),aura=J(g.playerAura),playArea=J(g.playArea),stages=g.stageConfigurations.Select(c=>new Stage{source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject),profile=J(c),advancedSource=AssetDatabase.GetAssetPath(c.advancedSettings),advanced=J(c.advancedSettings)}).ToArray()};
        File.WriteAllText(Folder+"/fasting-snapshot.json",JsonConvert.SerializeObject(result,Formatting.Indented));return "Read current fasting configuration and all 4 stage overrides";
    }
    public static string Scenes(int start,int count)
    {
        Directory.CreateDirectory(Folder);var output=new Rows();
        var paths=Directory.GetFiles("Assets/Scenes","*.unity",SearchOption.AllDirectories).Where(p=>!p.Contains("Prototypes")).OrderBy(p=>p).Skip(start).Take(count).ToArray();
        var seen=new HashSet<string>();
        foreach(var path in paths)
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(!component)continue;var script=MonoScript.FromMonoBehaviour(component);string scriptPath=AssetDatabase.GetAssetPath(script);
                    if(!scriptPath.StartsWith("Assets/Scripts/")||scriptPath.Contains("/Editor/"))continue;
                    var serialized=new SerializedObject(component);var p=serialized.GetIterator();
                    while(p.NextVisible(true))
                    {
                        if(p.propertyPath.StartsWith("m_")||p.propertyType==SerializedPropertyType.ObjectReference)continue;
                        string value=null;
                        switch(p.propertyType)
                        {
                            case SerializedPropertyType.Integer:value=p.longValue.ToString(System.Globalization.CultureInfo.InvariantCulture);break;
                            case SerializedPropertyType.Float:value=p.doubleValue.ToString("G9",System.Globalization.CultureInfo.InvariantCulture);break;
                            case SerializedPropertyType.Boolean:value=p.boolValue.ToString();break;
                            case SerializedPropertyType.Enum:value=p.enumValueIndex>=0&&p.enumValueIndex<p.enumDisplayNames.Length?p.enumDisplayNames[p.enumValueIndex]:p.intValue.ToString();break;
                        }
                        if(value==null||System.Text.RegularExpressions.Regex.IsMatch(p.propertyPath,@"\.(x|y|z|w|r|g|b|a)$"))continue;
                        if(p.propertyPath.EndsWith("Array.size")&&!System.Text.RegularExpressions.Regex.IsMatch(p.propertyPath,"frames|Frames|prices"))continue;
                        string key=path+"|"+component.GetType().Name+"|"+p.propertyPath+"|"+value;
                        if(seen.Add(key))output.rows.Add(new Row{scene=path.Replace('\\','/'),objectPath=Hierarchy(component.transform),component=component.GetType().Name,property=p.propertyPath,value=value,type=p.propertyType.ToString()});
                    }
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        File.WriteAllText(Folder+"/scenes-"+start+".json",JsonConvert.SerializeObject(output,Formatting.Indented));
        return "Inspected "+paths.Length+" scenes read-only; "+output.rows.Count+" serialized numeric/enum/bool entries; active scene kept unchanged.";
    }
    static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;
    public static string Cana()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Minigame2.unity");
        try
        {
            var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var s=components.OfType<CanaCustomerSpawner>().Single();
            var npcs=s.normalPrefabs.Concat(new[]{s.customerPrefab,s.gluttonPrefab,s.impatientPrefab,s.troublemakerPrefab,s.groupPrefab}).Concat(s.troublemakerPrefabs??Array.Empty<CanaCustomer>()).Where(n=>n).Distinct().ToArray();
            var data=new {player=J(components.OfType<CanaPlayer>().Single()),spawner=J(s),round=J(s.round),wallet=J(s.wallet),shop=J(components.OfType<CanaShop>().Single()),jesus=J(components.OfType<CanaJesus>().Single()),seats=s.seats.Length,complaintFrames=s.dissatisfaction.frames.Length,npcs=npcs.Select(n=>new {name=n.name,path=AssetDatabase.GetAssetPath(n),moveSpeed=n.moveSpeed,secondsPerGauge=n.secondsPerGauge,patienceFrames=n.gaugeFrames.Length,lastGreenFrame=n.lastGreenFrame,reactionSeconds=n.reactionSeconds}).ToArray()};
            File.WriteAllText(Folder+"/cana-snapshot.json",JsonConvert.SerializeObject(data,Formatting.Indented));
            return "Read Cana scene and "+npcs.Length+" linked NPC prefab templates without running gameplay";
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
