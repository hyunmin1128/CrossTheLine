using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using CrossTheLine.Minigames;
public static class InstallFastingStageTestPanel
{
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();var canvas=g.heading.canvas;
        const string path="Assets/Prefabs/Minigames/FastingTopDown/FastingStageTestPanel.prefab";
        var old=canvas.transform.Find("FastingStageTestPanel");if(old)throw new Exception("Panel already exists");
        var root=new GameObject("FastingStageTestPanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image),typeof(FastingStageTestPanel));
        try{
            var rt=(RectTransform)root.transform;rt.anchorMin=rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(1,1);rt.anchoredPosition=new Vector2(-12,-12);rt.sizeDelta=new Vector2(428,68);root.GetComponent<UnityEngine.UI.Image>().color=new Color(.08f,.07f,.06f,.9f);var panel=root.GetComponent<FastingStageTestPanel>();
            var title=new GameObject("Title",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));title.transform.SetParent(root.transform,false);var tr=(RectTransform)title.transform;tr.anchorMin=new Vector2(0,1);tr.anchorMax=Vector2.one;tr.pivot=new Vector2(.5f,1);tr.anchoredPosition=new Vector2(0,-4);tr.sizeDelta=new Vector2(0,20);var text=title.GetComponent<TextMeshProUGUI>();text.font=g.heading.font;text.fontSize=13;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;text.text="스테이지 테스트 · R: 선택한 스테이지 다시 시작";
            string[] names={"1 · 사막","2 · 동굴","3 · 폐유적","4 · 마지막 시련"};
            for(int i=0;i<4;i++){
                var b=UnityEngine.Object.Instantiate(g.startButton.gameObject,root.transform);b.name="Stage"+(i+1);var br=b.GetComponent<RectTransform>();br.anchorMin=br.anchorMax=new Vector2(0,0);br.pivot=new Vector2(0,0);br.anchoredPosition=new Vector2(8+i*104,8);br.sizeDelta=new Vector2(100,30);var button=b.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddIntPersistentListener(button.onClick,panel.StartStage,i);foreach(var label in b.GetComponentsInChildren<TMP_Text>(true)){label.text=names[i];label.fontSize=13;label.enableAutoSizing=false;label.raycastTarget=false;}
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform);instance.GetComponent<FastingStageTestPanel>().game=g;PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<FastingStageTestPanel>());instance.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Four stage buttons saved as Canvas prefab; direct start and retry keep selected test stage.";
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
