using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossTheLine.EditorTools
{
    // Game-view Gizmos is a local Editor preference, so each collaborator's machine can differ.
    [InitializeOnLoad]
    public static class FastingGameViewGuides
    {
        static readonly Dictionary<EditorWindow,bool> previous=new Dictionary<EditorWindow,bool>();
        static readonly Type gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        static PropertyInfo Gizmos=>gameView?.GetProperty("drawGizmos",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        static FastingGameViewGuides(){EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                for(int i=0;i<SceneManager.sceneCount;i++)
                    if(SceneManager.GetSceneAt(i).path.EndsWith("Chapter03_Fasting_TopDown_Test.unity",StringComparison.OrdinalIgnoreCase)){Hide();break;}
            }
            else if(state==PlayModeStateChange.ExitingPlayMode)Restore();
        }
        [MenuItem("CrossTheLine/Fasting/Hide Game View guides")]
        public static void Hide()
        {
            var property=Gizmos;if(property==null||!property.CanWrite)return;
            foreach(var value in Resources.FindObjectsOfTypeAll(gameView))
            {
                var window=value as EditorWindow;if(!window)continue;
                if(!previous.ContainsKey(window))previous[window]=(bool)property.GetValue(window);
                property.SetValue(window,false);window.Repaint();
            }
        }
        static void Restore()
        {
            var property=Gizmos;
            if(property!=null&&property.CanWrite)foreach(var entry in previous)if(entry.Key){property.SetValue(entry.Key,entry.Value);entry.Key.Repaint();}
            previous.Clear();
        }
    }
}
