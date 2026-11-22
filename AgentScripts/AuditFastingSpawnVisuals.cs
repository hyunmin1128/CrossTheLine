using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CrossTheLine.Minigames;
public static class AuditFastingSpawnVisuals
{
    public static string Run()
    {
        var ids=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Minigames/FastingTopDown"});
        var lines=new System.Collections.Generic.List<string>();
        foreach(var id in ids)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));var h=p.GetComponent<FastingTopDownHazard>();if(!h)continue;
            var frames=h.animationFrames;
            if(frames!=null&&frames.Length>0)
            {
                int missing=frames.Count(s=>!s);var xs=frames.Where(s=>s).Select(s=>s.bounds.size.x).ToArray();
                lines.Add(p.name+": frames="+frames.Length+", missing="+missing+", widths="+string.Join(",",xs.Select(x=>x.ToString("F2"))));
            }
        }
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
        lines.Add("Opacity shield="+g.effectSprites.shieldBubbleOpacity+", prayer="+g.effectSprites.prayerAuraOpacity+", fever="+g.settings.feverSeconds+"s");
        return string.Join("\n",lines);
    }
}
