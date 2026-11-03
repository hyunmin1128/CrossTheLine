using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CrossTheLine;
using System.Text.RegularExpressions;

// Explicit, local request file only; never changes assets without a requested action.
[InitializeOnLoad]
public static class JourneyAssetCleanup
{
    const string Output = "Docs/QA/AssetCleanup";
    static double nextRequest;
    static JourneyAssetCleanup() { EditorApplication.delayCall += Dispatch; EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextRequest || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextRequest = EditorApplication.timeSinceStartup + 2;
        if (File.Exists(Output + "/request.txt")) Dispatch();
    }
    static void Dispatch()
    {
        string request = Output + "/request.txt";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string action = File.ReadAllText(request).Trim();
        File.Delete(request);
        try
        {
            if (action == "audit") Audit();
            else if (action == "migrate") Migrate();
            else if (action == "resume") CompleteMoves();
            else if (action == "validate") Validate();
            else if (action == "repair-sprites") { RepairMissingSprites(); Validate(); }
            else if (action == "repair-library")
            {
                var library = AssetDatabase.LoadAssetAtPath<JourneyAssetLibrary>("Assets/Settings/CrossTheLine/JourneyAssetLibrary.asset");
                library.entries = library.entries.GroupBy(e => e.key).Select(g => new JourneyAssetLibrary.Entry { key = g.Key, assets = g.SelectMany(e => e.assets).Distinct().ToArray() }).ToList();
                EditorUtility.SetDirty(library); AssetDatabase.SaveAssets(); OrganizeSpriteDefinitions(); Validate();
            }
            else throw new Exception("Unknown cleanup action: " + action);
            File.WriteAllText(Output + "/status.txt", "PASS " + action + " " + DateTime.Now.ToString("O"));
        }
        catch (Exception e) { File.WriteAllText(Output + "/status.txt", "FAIL " + action + "\n" + e); Debug.LogException(e); }
    }
    [MenuItem("Tools/CrossTheLine/Cleanup/Audit Assets")]
    public static void Audit()
    {
        Directory.CreateDirectory(Output);
        var all = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
        var inbound = all.ToDictionary(p => p, p => new List<string>());
        foreach (string p in all)
            foreach (string d in AssetDatabase.GetDependencies(p, false))
                if (d != p && inbound.ContainsKey(d)) inbound[d].Add(p);
        File.WriteAllLines(Output + "/references-before.tsv", new[] { "path\tinbound\treferencedBy" }.Concat(all.Select(p => p + "\t" + inbound[p].Count + "\t" + string.Join(";", inbound[p]))));
        File.WriteAllLines(Output + "/zero-inbound-generated.tsv", all.Where(p => (p.StartsWith("Assets/Art/") || p.StartsWith("Assets/Resources/")) && inbound[p].Count == 0));
        File.WriteAllLines(Output + "/open-scenes.txt", Enumerable.Range(0, SceneManager.sceneCount).Select(i => { var s = SceneManager.GetSceneAt(i); return s.path + " dirty=" + s.isDirty; }));
        File.WriteAllText(Output + "/summary.txt", "Total assets: " + all.Length + "\nGenerated zero inbound: " + all.Count(p => (p.StartsWith("Assets/Art/") || p.StartsWith("Assets/Resources/")) && inbound[p].Count == 0));
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
    static string Destination(string p)
    {
        string ext = Path.GetExtension(p).ToLowerInvariant();
        if (p.StartsWith("Assets/Editor/")) return p.Replace("Assets/Editor/", "Assets/Scripts/Editor/");
        if (p.StartsWith("Assets/Art/CrossTheLine/"))
        {
            string rel = p.Substring("Assets/Art/CrossTheLine/".Length);
            if (ext == ".png" || ext == ".aseprite") return "Assets/Sprite/Generated/" + rel;
            if (ext == ".mat") return "Assets/Materials/CrossTheLine/" + Path.GetFileName(p);
            if (ext == ".anim" || ext == ".controller") return "Assets/Animation/CrossTheLine/" + Path.GetFileName(p);
            if (AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(Sprite)) return "Assets/Sprite/Generated/Definitions/" + Path.GetFileName(p);
            if (AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(Mesh)) return "Assets/Meshes/CrossTheLine/" + Path.GetFileName(p);
            return "Assets/Settings/CrossTheLine/" + Path.GetFileName(p);
        }
        if (p.StartsWith("Assets/Resources/"))
        {
            string rel = p.Substring("Assets/Resources/CrossTheLine/".Length);
            if (ext == ".png") return "Assets/Sprite/Generated/SourceAtlases/" + rel;
            if (ext == ".prefab") return "Assets/Prefabs/CrossTheLine/" + rel;
            if (ext == ".shader") return "Assets/Shaders/CrossTheLine/" + Path.GetFileName(p);
            if (ext == ".mat") return "Assets/Materials/CrossTheLine/" + Path.GetFileName(p);
            if (ext == ".wav") return "Assets/Audio/CrossTheLine/" + rel;
            if (ext == ".json") return "Assets/Data/CrossTheLine/" + Path.GetFileName(p);
            if (ext == ".asset" && AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(Texture2D)) return "Assets/Sprite/Generated/Palettes/" + Path.GetFileName(p);
            if (ext == ".txt") return "Assets/Data/CrossTheLine/" + rel;
            return "Assets/Settings/CrossTheLine/" + Path.GetFileName(p);
        }
        if (p.StartsWith("Assets/Prefabs/") && ext == ".mat") return "Assets/Materials/CrossTheLine/" + Path.GetFileName(p);
        if (p.StartsWith("Assets/Prefabs/") && ext == ".png") return "Assets/Sprite/Generated/Scenery/" + Path.GetFileName(p);
        return p;
    }
    static void CheckScenesSaved()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: " + SceneManager.GetSceneAt(i).path);
    }
    public static void Migrate()
    {
        CheckScenesSaved();
        if (!File.Exists("Docs/Backups/AssetCleanup-20261028/Assets-before.zip")) throw new Exception("Backup required.");
        var all = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
        File.WriteAllLines(Output + "/missing-guids-before.tsv", MissingGuids());
        var setup = EditorSceneManager.GetSceneManagerSetup();
        const string libraryPath = "Assets/Settings/CrossTheLine/JourneyAssetLibrary.asset";
        Folder("Assets/Settings/CrossTheLine");
        if (File.Exists(libraryPath)) throw new Exception("Migration already started; inspect report before retry.");
        var library = ScriptableObject.CreateInstance<JourneyAssetLibrary>();
        foreach (var p in all.Where(p => p.StartsWith("Assets/Resources/") && Path.GetExtension(p) != ".txt"))
        {
            string key = p.Substring("Assets/Resources/".Length);
            key = key.Substring(0, key.Length - Path.GetExtension(key).Length);
            var assets = Resources.LoadAll<UnityEngine.Object>(key);
            if (assets.Length == 0) assets = AssetDatabase.LoadAllAssetsAtPath(p).Where(a => a && !(a is MonoScript)).ToArray();
            if (assets.Length == 0) throw new Exception("Cannot capture resource: " + p);
            library.entries.Add(new JourneyAssetLibrary.Entry { key = key, assets = assets });
        }
        library.entries = library.entries.GroupBy(e => e.key).Select(g => new JourneyAssetLibrary.Entry { key = g.Key, assets = g.SelectMany(e => e.assets).Distinct().ToArray() }).ToList();
        AssetDatabase.CreateAsset(library, libraryPath);
        AssetDatabase.SaveAssets();
        File.WriteAllLines(Output + "/resource-map.tsv", new[] { "key\tobjects" }.Concat(library.entries.Select(e => e.key + "\t" + string.Join(";", e.assets.Select(a => a.GetType().Name + ":" + a.name)))));
        int games = 0;
        foreach (var scenePath in all.Where(p => p.EndsWith(".unity")))
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var controllers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<JourneyGame>(true)).ToArray();
            foreach (var g in controllers) { g.runtimeAssets = library; EditorUtility.SetDirty(g); games++; }
            if (controllers.Length > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        foreach (var path in all.Where(p => p.EndsWith(".prefab")))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab || prefab.GetComponentsInChildren<JourneyGame>(true).Length == 0) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { foreach (var g in root.GetComponentsInChildren<JourneyGame>(true)) { g.runtimeAssets = library; games++; } PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        EditorSceneManager.RestoreSceneManagerSetup(setup);
        AssetDatabase.SaveAssets();

        // Keep everything reachable from any retained project asset or runtime library.
        // User-authored Sprite/Aseprite files and non-generated prefabs are always roots.
        var roots = all.Where(p => !p.StartsWith("Assets/Art/") && !p.StartsWith("Assets/Resources/")).Concat(new[] { libraryPath }).ToArray();
        var reachable = new HashSet<string>(AssetDatabase.GetDependencies(roots, true));
        var deleted = new List<string>();
        var candidates = all.Where(p => p.StartsWith("Assets/Art/") && !reachable.Contains(p)).ToArray();
        foreach (var p in candidates)
        {
            if (!Path.GetFullPath(p).StartsWith(Path.GetFullPath("Assets/Art/").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe delete: " + p);
            if (!AssetDatabase.DeleteAsset(p)) throw new Exception("Delete failed: " + p);
            deleted.Add(p);
        }
        File.WriteAllLines(Output + "/deleted.tsv", deleted);
        var moves = all.Where(p => !deleted.Contains(p)).Select(p => new { source = p, target = Destination(p) }).Where(m => m.source != m.target).ToArray();
        var duplicates = moves.GroupBy(m => m.target).Where(g => g.Count() > 1).ToArray();
        if (duplicates.Length > 0) throw new Exception("Destination collisions: " + string.Join(",", duplicates.Select(g => g.Key)));
        foreach (var m in moves) if (File.Exists(m.target)) throw new Exception("Destination exists: " + m.target);
        File.WriteAllLines(Output + "/moves.tsv", new[] { "source\ttarget\tguid" }.Concat(moves.Select(m => m.source + "\t" + m.target + "\t" + AssetDatabase.AssetPathToGUID(m.source))));
        CompleteMoves();
        File.WriteAllText(Output + "/migration.txt", "Moved: " + moves.Length + "\nDeleted generated assets: " + deleted.Count + "\nBound game controllers: " + games + "\nRuntime keys: " + library.entries.Count);
        Validate();
    }
    static void CompleteMoves()
    {
        CheckScenesSaved();
        var moves = File.ReadAllLines(Output + "/moves.tsv").Skip(1).Select(row => row.Split('\t')).ToArray();
        // Folder creation must finish importing before batching moves.
        foreach (var m in moves) Folder(Path.GetDirectoryName(m[1]).Replace('\\', '/'));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var m in moves.OrderBy(m => m[0].EndsWith("JourneyAssetCleanup.cs") ? 1 : 0))
            {
                if (!File.Exists(m[0]) && AssetDatabase.AssetPathToGUID(m[1]) == m[2]) continue;
                string error = AssetDatabase.MoveAsset(m[0], m[1]);
                if (!string.IsNullOrEmpty(error)) throw new Exception(m[0] + " -> " + m[1] + ": " + error);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        foreach (var folder in new[] { "Assets/Art", "Assets/Resources", "Assets/Editor", "Assets/Docs" }) RemoveEmpty(folder);
        File.WriteAllText(Output + "/migration.txt", "Moved: " + moves.Length + "\nDeleted generated assets: " + File.ReadAllLines(Output + "/deleted.tsv").Length);
        Validate();
    }
    static void RemoveEmpty(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (var sub in AssetDatabase.GetSubFolders(folder)) RemoveEmpty(sub);
        if (!Directory.EnumerateFileSystemEntries(folder).Any()) AssetDatabase.DeleteAsset(folder);
    }
    static void OrganizeSpriteDefinitions()
    {
        string dir = "Assets/Sprite/Generated/Definitions";
        Folder(dir);
        var rows = File.ReadAllLines(Output + "/moves.tsv").ToList();
        for (int i = 1; i < rows.Count; i++)
        {
            var parts = rows[i].Split('\t');
            if (!parts[1].StartsWith("Assets/Settings/CrossTheLine/") || AssetDatabase.GetMainAssetTypeAtPath(parts[1]) != typeof(Sprite)) continue;
            string target = dir + "/" + Path.GetFileName(parts[1]);
            string error = AssetDatabase.MoveAsset(parts[1], target);
            if (!string.IsNullOrEmpty(error)) throw new Exception(error);
            parts[1] = target;
            rows[i] = string.Join("\t", parts);
        }
        File.WriteAllLines(Output + "/moves.tsv", rows);
        AssetDatabase.SaveAssets();
    }
    static void RepairMissingSprites()
    {
        CheckScenesSaved();
        const string catalogPath = "Assets/Settings/CrossTheLine/JourneySpriteCatalog.asset";
        // Reconstruct the pre-repair serialized files, retaining the new library binding.
        // The original ZIP is authoritative for references that Unity had cached in memory.
        string previousReport = Output + "/repaired-preexisting-sprites.tsv";
        if (File.Exists(previousReport) && !File.Exists(Output + "/reference-repair-restored.txt"))
        {
            var moves = File.ReadAllLines(Output + "/moves.tsv").Skip(1).Select(row => row.Split('\t')).ToDictionary(p => p[1], p => p[0]);
            string libraryGuid = AssetDatabase.AssetPathToGUID("Assets/Settings/CrossTheLine/JourneyAssetLibrary.asset");
            using (var zip = System.IO.Compression.ZipFile.OpenRead("Docs/Backups/AssetCleanup-20261028/Assets-before.zip"))
            {
                foreach (var row in File.ReadAllLines(previousReport).Skip(1))
                {
                    string p = row.Split('\t')[0].Replace('\\', '/');
                    string original = moves.TryGetValue(p, out var from) ? from : p;
                    string relative = original.Substring("Assets/".Length);
                    var entry = zip.Entries.First(e => e.FullName.Replace('\\', '/') == relative);
                    string text;
                    using (var reader = new StreamReader(entry.Open())) text = reader.ReadToEnd();
                    if (p.EndsWith(".unity") || p.EndsWith(".prefab")) text = Regex.Replace(text, @"(?m)^(  storySource: [^\r\n]+)", "$1\n  runtimeAssets: {fileID: 11400000, guid: " + libraryGuid + ", type: 2}");
                    File.WriteAllText(p, text, new System.Text.UTF8Encoding(false));
                }
            }
            File.WriteAllText(Output + "/reference-repair-restored.txt", "Restored pre-repair references from backup; retained scene runtimeAssets bindings.");
        }
        var remap = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(File.ReadAllText(catalogPath), @"- original: (\{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: [23]\})\r?\n\s+resource:[^\r\n]*\r?\n\s+pixel: (\{fileID: -?\d+, guid: ([0-9a-f]{32}), type: [23]\})"))
        {
            string oldPath = AssetDatabase.GUIDToAssetPath(m.Groups[3].Value);
            string target = AssetDatabase.GUIDToAssetPath(m.Groups[5].Value);
            if (!File.Exists(oldPath) && File.Exists(target)) remap[m.Groups[1].Value] = m.Groups[4].Value;
        }
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var records = new List<string> { "path\trepairedReferences" };
        var changed = new List<string>();
        foreach (string p in Directory.GetFiles("Assets", "*", SearchOption.AllDirectories))
        {
            if (!new[] { ".unity", ".prefab", ".asset", ".mat", ".anim", ".controller" }.Contains(Path.GetExtension(p))) continue;
            string text = File.ReadAllText(p);
            if (!text.StartsWith("%YAML")) continue;
            int count = 0;
            string fixedText = Regex.Replace(text, @"\{fileID: -?\d+, guid: [0-9a-f]{32}, type: [23]\}", m => { if (!remap.TryGetValue(m.Value, out var replacement)) return m.Value; count++; return replacement; });
            if (count == 0) continue;
            File.WriteAllText(p, fixedText, new System.Text.UTF8Encoding(false));
            changed.Add(p.Replace('\\', '/')); records.Add(p + "\t" + count);
        }
        File.WriteAllLines(Output + "/repaired-preexisting-sprites.tsv", records);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var p in changed) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        EditorSceneManager.RestoreSceneManagerSetup(setup);
        JourneySpriteAssets.Reset();
    }
    static string[] MissingGuids()
    {
        var known = new HashSet<string>(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Packages/") || File.Exists(p) || Directory.Exists(p)).Select(AssetDatabase.AssetPathToGUID));
        var lines = new List<string>();
        foreach (var p in Directory.GetFiles("Assets", "*", SearchOption.AllDirectories).Concat(Directory.GetFiles("ProjectSettings", "*", SearchOption.AllDirectories)))
        {
            string ext = Path.GetExtension(p).ToLowerInvariant();
            if (!new[] { ".unity", ".prefab", ".asset", ".mat", ".anim", ".controller", ".meta", ".shadergraph" }.Contains(ext)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(p), @"guid:\s*([a-fA-F0-9]{32})"))
            {
                string guid = m.Groups[1].Value.ToLowerInvariant();
                if (!guid.StartsWith("0000000000000000") && !known.Contains(guid)) lines.Add(p.Replace('\\', '/') + "\t" + guid);
            }
        }
        return lines.Distinct().OrderBy(s => s).ToArray();
    }
    public static void Validate()
    {
        var report = new List<string>();
        var library = AssetDatabase.LoadAssetAtPath<JourneyAssetLibrary>("Assets/Settings/CrossTheLine/JourneyAssetLibrary.asset");
        if (!library) throw new Exception("Runtime library missing.");
        if (library.entries.GroupBy(e => e.key).Any(g => g.Count() > 1)) throw new Exception("Duplicate runtime keys.");
        foreach (var e in library.entries) if (e.assets == null || e.assets.Length == 0 || e.assets.Any(a => !a)) throw new Exception("Missing runtime object: " + e.key);
        JourneyAssets.Bind(library);
        foreach (var e in library.entries)
        {
            var actual = JourneyAssets.LoadAll<UnityEngine.Object>(e.key);
            if (!actual.SequenceEqual(e.assets)) throw new Exception("Resource lookup changed: " + e.key);
        }
        report.Add("PASS " + library.entries.Count + " runtime keys and subasset references");
        var before = new HashSet<string>(File.ReadAllLines(Output + "/missing-guids-before.tsv"));
        var after = MissingGuids();
        File.WriteAllLines(Output + "/missing-guids-after.tsv", after);
        // Existing paths may have moved; compare the missing GUID set rather than filenames.
        var oldGuids = new HashSet<string>(before.Select(s => s.Split('\t').Last()));
        var added = after.Where(s => !oldGuids.Contains(s.Split('\t').Last())).ToArray();
        if (added.Length > 0) throw new Exception("New missing GUIDs: " + string.Join(";", added.Take(10)));
        report.Add("PASS no new missing GUIDs (preexisting: " + before.Count + ", current: " + after.Length + ")");
        foreach (var row in File.ReadAllLines(Output + "/moves.tsv").Skip(1))
        {
            var parts = row.Split('\t');
            if (AssetDatabase.AssetPathToGUID(parts[1]) != parts[2]) throw new Exception("GUID changed: " + parts[1]);
        }
        report.Add("PASS all moved asset GUIDs preserved");
        foreach (var scene in EditorBuildSettings.scenes) if (!File.Exists(scene.path)) throw new Exception("Missing build scene: " + scene.path);
        report.Add("PASS " + EditorBuildSettings.scenes.Length + " build scene paths");
        int checkedObjects = 0, checkedGames = 0;
        Action<GameObject> inspect = root =>
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script: " + t.name);
                checkedObjects++;
            }
            foreach (var game in root.GetComponentsInChildren<JourneyGame>(true))
            {
                if (game.runtimeAssets != library) throw new Exception("Unbound runtime library: " + game.name);
                checkedGames++;
            }
        };
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.EndsWith(".prefab"))) inspect(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.EndsWith(".unity")))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) inspect(root);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        report.Add("PASS " + checkedObjects + " objects: no missing scripts; " + checkedGames + " game controllers bound");
        File.WriteAllLines(Output + "/validation.txt", report);
    }
}
