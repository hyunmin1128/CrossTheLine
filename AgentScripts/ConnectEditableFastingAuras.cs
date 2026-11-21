using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using CrossTheLine.Minigames;
public static class ConnectEditableFastingAuras
{
    const string Folder="Assets/Sprite/Effect/Fasting/Editable/";
    static Sprite[] Frames(string name)
    {
        string path=Folder+name+".aseprite";
        var importer=(AsepriteImporter)AssetImporter.GetAtPath(path);
        importer.importMode=FileImportModes.AnimatedSprite;importer.layerImportMode=LayerImportModes.MergeFrame;
        importer.spritePixelsPerUnit=32;importer.pivotSpace=PivotSpaces.Canvas;importer.pivotAlignment=SpriteAlignment.Center;
        importer.spriteMeshType=SpriteMeshType.FullRect;importer.generateAnimationClips=true;importer.generateModelPrefab=true;
        importer.filterMode=FilterMode.Point;importer.SaveAndReimport();
        var assets=AssetDatabase.LoadAllAssetsAtPath(path);var clip=assets.OfType<AnimationClip>().FirstOrDefault(c=>c.name=="Loop");
        if(clip)
        {
            var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b=>b.propertyName=="m_Sprite");
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            int count=keys.Length;
            if(count>1&&keys[count-1].value==keys[count-2].value)count--;
            return keys.Take(count).Select(k=>(Sprite)k.value).ToArray();
        }
        var sprites=assets.OfType<Sprite>().ToArray();if(sprites.Length==0)throw new Exception("No effect sprites in "+path);return sprites;
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        const string path="Assets/Settings/Minigames/FastingEffectSprites.asset";
        Directory.CreateDirectory("Docs/Backups/EditableAuras-20261121");
        if(!File.Exists("Docs/Backups/EditableAuras-20261121/FastingEffectSprites.asset"))File.Copy(path,"Docs/Backups/EditableAuras-20261121/FastingEffectSprites.asset");
        var art=AssetDatabase.LoadAssetAtPath<FastingEffectSprites>(path);
        art.bubble=Frames("ShieldBubble");art.prayer=Frames("PrayerLight");art.prayerAura=Frames("PrayerAura");
        art.orbitShield=Frames("OrbitShield")[0];art.spark=Frames("ShieldAcquireSpark")[0];art.shieldBreak=Frames("ShieldBreak");
        art.editableAuraSprites=true;EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
        return "Connected Aseprite subassets directly: bubble="+art.bubble.Length+", prayer="+art.prayer.Length+", aura="+art.prayerAura.Length+", break="+art.shieldBreak.Length+"; merged layers, canvas-centered pivots, 32PPU, Point filtering. PNG exports are no longer needed for these effects.";
    }
}
