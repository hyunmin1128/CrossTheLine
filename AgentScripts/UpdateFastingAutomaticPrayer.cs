using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using CrossTheLine.Minigames;
public static class UpdateFastingAutomaticPrayer
{
    static void Labels(GameObject root,bool instance)
    {
        foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)){
            if(t.name=="FiveItemGuide")t.text="자동 전진하며 기도량이 기본 초당 1씩 충전됩니다.\n좌우 키를 누르면 한 칸 즉시 이동합니다. 위아래 이동 불가\n기도 방해 중에는 충전 정지 / 남은 시간이 표시됩니다.\n\n1. 수통 [E] : 정화 + 지침 1칸 회복 + 잠시 가속\n2. 믿음의 방패 : 다음 충돌 자동 방어 후 소모\n3. 성령의 불꽃 : 자동 기도 가속 / 3개마다 피버\n4. 완본 100% / 페이지 25~40% / 조각 10~25%\n5. 물 보급 : 수통 충전 + 보호 (마지막 구간 없음)\n\n목표를 채우면 앞에 나타나는 금색 선을 통과하세요.\n파란 칸: 엄폐 범위 / 초록 칸: 해당 방향 엄폐 적용\n지침 5칸 · 피해 1칸 · 실패 시 1일부터 재시작";
            else if(t.name=="Controls")t.text="A/D 또는 ←/→ : 한 칸 회피 · 기도 자동 충전 · E 수통 · R 재시작";
            else if(t.name=="Feedback")t.text="자동 전진 / 자동 기도 · 좌우 한 칸 회피";
            else continue;
            if(instance)PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);
        }
    }
    public static string Run(){if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved scene");string p="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";var root=PrefabUtility.LoadPrefabContents(p);Labels(root,false);PrefabUtility.SaveAsPrefabAsset(root,p);PrefabUtility.UnloadPrefabContents(root);Labels(g.introduction.transform.parent.gameObject,true);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Automatic prayer instructions saved in scene and HUD prefab";}
}
