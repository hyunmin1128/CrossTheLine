using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CrossTheLine.Minigames;
public static class RemoveHazardTelegraphs
{
    static void Clean(FastingTopDownHazard h)
    {
        foreach(var r in h.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r!=h.body&&(r==h.footprint||r==h.arrow||r.name.StartsWith("HitAreaBorder_"))).ToArray())if(r)UnityEngine.Object.DestroyImmediate(r.gameObject);
        h.footprint=h.arrow=null;h.warning=0;h.lifetime=30;h.body.enabled=true;
        string path=AssetDatabase.GetAssetPath(h.body.sprite);
        if(path.EndsWith(".png",StringComparison.OrdinalIgnoreCase)){var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(path));int min=t.width,max=0;var rect=h.body.sprite.rect;for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(t.GetPixel(x,y).a>.5f){min=Math.Min(min,x);max=Math.Max(max,x);}h.artworkWidth=(max-min+1)/h.body.sprite.pixelsPerUnit;UnityEngine.Object.DestroyImmediate(t);}
    }
    static void Guide(GameObject root){foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)){if(t.text.Contains("붉은 바닥")){t.text=string.Join("\n",t.text.Split('\n').Where(s=>!s.Contains("붉은 바닥")));t.text+="\n장애물은 위에서 등장합니다. 모래바람 1~2길 / 돌멩이 1길";PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);}}}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved scene");
        var prefabs=new[]{g.hazardPrefab,g.desertArt.rockPrefab,g.desertArt.sandstormPrefab,g.desertArt.cactusPrefab,g.desertArt.quicksandPrefab};foreach(var prefab in prefabs){string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);Clean(root.GetComponent<FastingTopDownHazard>());PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
        g.feet.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(g.feet.gameObject);foreach(var lane in g.laneGuides){lane.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(lane.gameObject);}var hud=g.introduction.transform.parent.gameObject;Guide(hud);string hp="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";var contents=PrefabUtility.LoadPrefabContents(hp);Guide(contents);PrefabUtility.SaveAsPrefabAsset(contents,hp);PrefabUtility.UnloadPrefabContents(contents);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Removed ground rectangles, arrows and debug lane/feet guides; five hazard prefabs updated.";
    }
}
