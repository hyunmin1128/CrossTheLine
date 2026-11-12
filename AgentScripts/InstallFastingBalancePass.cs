using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrossTheLine.Minigames;
using TMPro;
public static class InstallFastingBalancePass
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Saved scene needed");
        var s=g.settings;s.prayerTarget=90;s.bibleSpawnChance=.01f;s.pageSpawnChance=.10f;s.pieceSpawnChance=.18f;s.prayerTargets=new float[]{90,100,110,120};s.bibleChances=new float[]{.01f,.01f,.01f,.01f};s.pageChances=new float[]{.10f,.12f,.14f,.16f};s.pieceChances=new float[]{.18f,.20f,.22f,.24f};s.wellChances=new float[]{.65f,.60f,.55f,0};s.wellIntervalSeconds=new float[]{18,20,22,24};s.attackSeconds=new float[]{5,5.5f,6,6.5f};s.restSeconds=new float[]{3,2.8f,2.6f,2.4f};s.waveIntervals=new float[]{1.6f,1.45f,1.3f,1.15f};s.flameChance=.30f;s.shieldChance=.16f;
        EditorUtility.SetDirty(s);
        string path="Assets/Prefabs/Minigames/FastingTopDown/FastingEncounterDirector.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab){var o=new GameObject("EncounterDirector",typeof(FastingEncounterDirector));prefab=PrefabUtility.SaveAsPrefabAsset(o,path);UnityEngine.Object.DestroyImmediate(o);}
        if(!g.encounterDirector)g.encounterDirector=((GameObject)PrefabUtility.InstantiatePrefab(prefab,g.transform)).GetComponent<FastingEncounterDirector>();
        g.encounterDirector.game=g;PrefabUtility.RecordPrefabInstancePropertyModifications(g.encounterDirector);PrefabUtility.RecordPrefabInstancePropertyModifications(g);
        string hudPath="Assets/Prefabs/Minigames/FastingTopDown/FastingTopDownHUD.prefab";
        var root=PrefabUtility.LoadPrefabContents(hudPath);var hud=root.GetComponent<FastingPortraitHUD>();var card=root.transform.Find("PortraitCard");
        if(!hud.prayerState)
        {
            var o=new GameObject("PrayerState",typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image));o.transform.SetParent(card,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(142,108);r.sizeDelta=new Vector2(284,4);hud.prayerState=o.GetComponent<UnityEngine.UI.Image>();hud.prayerState.sprite=g.prayerFill.sprite;hud.prayerState.type=UnityEngine.UI.Image.Type.Filled;hud.prayerState.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;hud.prayerState.raycastTarget=false;
        }
        PrefabUtility.SaveAsPrefabAsset(root,hudPath);PrefabUtility.UnloadPrefabContents(root);
        foreach(var text in g.introduction.GetComponentsInChildren<TMP_Text>(true))if(text.name=="FiveItemGuide"){text.text="WASD / 방향키: 이동\nSpace 유지: 0.3초 준비 후 기도 · 이동속도 15%\n공격이 지나간 뒤 청록색 표시가 켜지면 기도할 틈입니다.\nE: 수통 사용 · R: 처음부터\n\n성경 완본: 이 구간 기도 완료\n페이지 / 조각: 기도 게이지 일부 충전\n성령불: 기도 가속 · 3개로 피버 (기도할 때만 시간 소모)\n방패: 다음 피격 자동 방어\n우물: 빈 수통을 한 번 충전 · 위치/등장은 무작위\n실패하면 다시 시작 버튼으로 재도전할 수 있습니다.";PrefabUtility.RecordPrefabInstancePropertyModifications(text);}
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
        return "Balance baseline, encounter prefab and prayer-state UI saved.";
    }
}
