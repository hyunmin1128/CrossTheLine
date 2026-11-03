using System.Collections.Generic;
using CrossTheLine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrossTheLine.EditorTools
{
    // Preview-only fallback. Serialized scene references and font files stay unchanged.
    [InitializeOnLoad]
    public static class JourneyEditorFontPreview
    {
        static readonly HashSet<TMP_FontAsset> attached = new HashSet<TMP_FontAsset>();
        static readonly HashSet<TMP_Text> seen = new HashSet<TMP_Text>();
        static TMP_FontAsset fallback;
        static bool suspended;
        static double nextRefresh;

        static JourneyEditorFontPreview()
        {
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
            EditorApplication.playModeStateChanged += state =>
            {
                if(state == PlayModeStateChange.ExitingEditMode) Dispose();
                if(state == PlayModeStateChange.EnteredEditMode) { Dispose(); suspended=false; nextRefresh=0; Refresh(); }
            };
            EditorSceneManager.sceneOpened += (scene, mode) => nextRefresh=0;
        }

        static void Update()
        {
            if(suspended || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh=EditorApplication.timeSinceStartup+.5;
            Refresh();
        }

        public static void Refresh()
        {
            if(suspended || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!JourneyRuntimeFonts.IsUsable(fallback)) { Detach(); fallback=JourneyRuntimeFonts.SystemKorean; }
            if(!fallback) return;
            foreach(var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if(!text || !text.gameObject.scene.IsValid() || !text.font || !AssetDatabase.Contains(text.font)) continue;
                var font=text.font;
                if(font.fallbackFontAssetTable==null) font.fallbackFontAssetTable=new List<TMP_FontAsset>();
                if(!font.fallbackFontAssetTable.Contains(fallback))
                {
                    font.fallbackFontAssetTable.Add(fallback);
                    attached.Add(font);
                    TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true,font);
                }
                if(seen.Add(text)) text.ForceMeshUpdate(true);
            }
            SceneView.RepaintAll();
        }

        public static void BeforeSave()
        {
            suspended=true;
            Detach();
            EditorApplication.delayCall += () => { suspended=false; nextRefresh=0; Refresh(); };
        }

        static void Detach()
        {
            foreach(var font in attached) if(font && font.fallbackFontAssetTable!=null) font.fallbackFontAssetTable.Remove(fallback);
            attached.Clear();
            seen.Clear();
        }

        static void Dispose()
        {
            suspended=true;
            Detach();
            JourneyRuntimeFonts.ClearTemporaryFonts();
            fallback=null;
        }
    }

    public sealed class JourneyEditorFontSaveGuard : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            JourneyEditorFontPreview.BeforeSave();
            return paths;
        }
    }
}
