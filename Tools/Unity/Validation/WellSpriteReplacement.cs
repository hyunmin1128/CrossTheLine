using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CrossTheLine.Minigames;

// Explicit request only. Changes renderer artwork without changing water gameplay.
[InitializeOnLoad]
public static class WellSpriteReplacement
{
    const string Source = "Assets/Sprite/Objects/Well.png";
    const string Old = "Assets/Sprite/Minigames/FastingTopDown/TempWater.png";
    const string Report = "Docs/QA/WellSpriteReplacement";
    const string PlayFlag = "CrossTheLine.VerifyWellSprite";
    static double nextPoll;
    static WellSpriteReplacement()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PlayFlag, false)) VerifyPlay();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(PlayFlag, false)) SessionState.SetBool(PlayFlag, false);
        };
    }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string request = Report + "/request.txt";
        if (!File.Exists(request)) return;
        string action = File.ReadAllText(request).Trim(); File.Delete(request);
        try
        {
            if (action == "apply") Apply();
            else if (action == "play") { SessionState.SetBool(PlayFlag, true); EditorApplication.isPlaying = true; }
            else throw new Exception("Unknown well request.");
        }
        catch (Exception ex) { File.WriteAllText(Report + "/status.txt", "FAIL " + ex); Debug.LogException(ex); }
    }
    static void Backup(string path, string root)
    {
        string target = root + "/" + path;
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        if (!File.Exists(target)) File.Copy(path, target);
    }
    static Sprite UserSprite() => AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Sprite>().Single();
    static bool IsOld(Sprite sprite) => sprite && AssetDatabase.GetAssetPath(sprite) == Old;
    static int Replace(GameObject root, Sprite replacement, List<string> rows)
    {
        int changed = 0;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!IsOld(renderer.sprite)) continue;
            var before = renderer.bounds;
            var oldScale = renderer.transform.localScale;
            float worldScale = Mathf.Abs(renderer.transform.lossyScale.x / oldScale.x);
            float scale = before.size.x / (replacement.bounds.size.x * worldScale);
            renderer.sprite = replacement;
            renderer.color = Color.white;
            renderer.transform.localScale = new Vector3(Mathf.Sign(oldScale.x) * scale, Mathf.Sign(oldScale.y) * scale, oldScale.z);
            var after = renderer.bounds;
            renderer.transform.position += new Vector3(before.center.x - after.center.x, before.min.y - after.min.y, 0);
            EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(renderer.transform);
            if (PrefabUtility.IsPartOfPrefabInstance(renderer))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
            }
            rows.Add(renderer.gameObject.scene.path + "\t" + renderer.transform.parent.name + "/" + renderer.name + "\t" + before.size + "\t" + renderer.bounds.size);
            changed++;
        }
        return changed;
    }
    static void Apply()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save open scene first: " + SceneManager.GetSceneAt(i).path);
        Directory.CreateDirectory(Report);
        string backup = "Docs/Backups/WellSpriteReplacement-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string guid = AssetDatabase.AssetPathToGUID(Old);
        var candidates = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && (p.EndsWith(".unity") || p.EndsWith(".prefab")) && File.ReadAllText(p).Contains(guid)).ToArray();
        if (candidates.Length == 0) throw new Exception("No original water sprites found.");
        foreach (var p in candidates) Backup(p, backup);
        Backup(Source + ".meta", backup);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Source);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 32;
        importer.alphaIsTransparency = true;
        // Keep the user's existing sprite rectangle, mode, pivot, GUID and local ID.
        importer.SaveAndReimport();
        var sprite = UserSprite();
        var rows = new List<string> { "scene\tobject\tpreviousSize\tnewSize" };
        int total = 0;
        try
        {
            foreach (var p in candidates)
            {
                if (p.EndsWith(".prefab"))
                {
                    var root = PrefabUtility.LoadPrefabContents(p);
                    try { int count = Replace(root, sprite, rows); if (count > 0) PrefabUtility.SaveAsPrefabAsset(root, p); total += count; }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                else
                {
                    var scene = EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                    int count = scene.GetRootGameObjects().Sum(r => Replace(r, sprite, rows));
                    if (count > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                    total += count;
                }
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        AssetDatabase.SaveAssets();
        var remaining = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset")) && File.ReadAllText(p).Contains(guid)).ToArray();
        if (remaining.Length > 0) throw new Exception("Original water sprite references remain: " + string.Join(", ", remaining));
        File.WriteAllLines(Report + "/replacements.tsv", rows);
        File.WriteAllText(Report + "/status.txt", "PASS replaced " + total + " water sprites. Backup: " + backup + ". Sprite name: " + sprite.name);
    }
    static async Task Frames(int n) { int end = Time.frameCount + n; while (Time.frameCount < end) await Task.Yield(); }
    static async void VerifyPlay()
    {
        try
        {
            await Frames(5);
            var game = UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();
            if (!game) throw new Exception("Open the fasting top-down scene for verification.");
            var waters = game.entities.Where(e => e && e.kind == FastingTopDownEntity.Kind.Water).ToArray();
            if (waters.Length != 3) throw new Exception("Expected three water points, found " + waters.Length);
            foreach (var water in waters)
            {
                var symbol = water.transform.Find("Symbol").GetComponent<SpriteRenderer>();
                if (!symbol.sprite || AssetDatabase.GetAssetPath(symbol.sprite) != Source) throw new Exception("Wrong water artwork: " + water.name);
                if (symbol.sprite.texture.filterMode != FilterMode.Point) throw new Exception("Well filtering changed.");
                if (!water.Contains(water.transform.position)) throw new Exception("Water pickup footprint is invalid.");
            }
            game.Begin(); game.enabled = false;
            var first = waters.Single(e => e.stage == 0);
            typeof(FastingTopDownGame).GetField("cameraY", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, first.transform.position.y);
            game.player.position = first.transform.position;
            game.UseWater();
            if (game.HasWater) throw new Exception("Water use did not consume water before test.");
            game.Tick(.01f, Vector2.zero);
            if (!game.HasWater) throw new Exception("Well no longer refills water.");
            game.player.position = first.transform.position + new Vector3(1.3f, -.5f, 0);
            await Frames(3);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report + "/well-in-game.png"));
            await Frames(3);
            File.WriteAllText(Report + "/play-validation.txt", "PASS 3 water points use user well; point filtering; pickup bounds; water consumption and refill; advancing Play frames.");
        }
        catch (Exception ex) { File.WriteAllText(Report + "/play-validation.txt", "FAIL " + ex); Debug.LogException(ex); }
        finally { EditorApplication.isPlaying = false; }
    }
}
