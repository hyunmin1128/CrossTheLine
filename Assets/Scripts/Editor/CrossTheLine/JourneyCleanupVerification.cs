using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CrossTheLine;

[InitializeOnLoad]
public static class JourneyCleanupVerification
{
    const string Root = "Docs/QA/AssetCleanup";
    const string Flag = "Journey.AssetCleanupSmoke";
    static bool running;
    static JourneyCleanupVerification()
    {
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.delayCall += BeginRequested;
        EditorApplication.update += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating) BeginRequested(); };
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ProtectSave() { if (SessionState.GetBool(Flag, false)) JourneySceneFlow.TestWithoutSaving = true; }
    static string HashSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "CrossTheLine-journey.json");
        using (var sha = SHA256.Create()) return File.Exists(path) ? Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))) : "absent";
    }
    [MenuItem("Tools/CrossTheLine/Cleanup/Verify All Chapters")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save open scenes first.");
        Directory.CreateDirectory(Root);
        SessionState.SetString(Flag + ".save", HashSave());
        SessionState.SetBool(Flag, true);
        EditorSceneManager.OpenScene("Assets/Scenes/CrossTheLineJourney.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    static void BeginRequested()
    {
        if (!File.Exists(Root + "/playcheck.request")) return;
        File.Delete(Root + "/playcheck.request");
        try { Begin(); } catch (Exception e) { File.WriteAllText(Root + "/playcheck.txt", "FAIL " + e); }
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode && !running) Run();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            bool preserved = HashSave() == SessionState.GetString(Flag + ".save", "");
            File.AppendAllText(Root + "/playcheck.txt", "\n" + (preserved ? "PASS" : "FAIL") + " user save preserved\n");
            SessionState.SetBool(Flag, false);
            JourneySceneFlow.TestWithoutSaving = false;
        }
    }
    static async Task Frames(int count)
    {
        int target = Time.frameCount + count;
        float start = Time.realtimeSinceStartup;
        while (Time.frameCount < target)
        {
            if (Time.realtimeSinceStartup - start > 15) throw new Exception("Play frames stalled.");
            await Task.Yield();
        }
    }
    static async void Run()
    {
        running = true;
        var rows = new List<string>();
        var errors = new List<string>();
        Application.LogCallback log = (message, trace, type) => { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message); };
        Application.logMessageReceived += log;
        try
        {
            JourneySceneFlow.TestWithoutSaving = true;
            await Frames(5);
            var game = UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
            if (!game || !game.runtimeAssets) throw new Exception("Main game/library missing.");
            var world = game.GetComponent<JourneyContinuousWorld>();
            if (!world) throw new Exception("Continuous world missing.");
            var book = JsonUtility.FromJson<StoryBook>(game.storySource.text);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Action<string, object[]> call = (name, args) => typeof(JourneyGame).GetMethod(name, flags).Invoke(game, args);
            int checks = 0;
            for (int chapter = 0; chapter < book.chapters.Length; chapter++)
            {
                for (int step = 0; step < book.chapters[chapter].steps.Length; step++)
                {
                    world.Normalize();
                    call("ClearStoryPanels", Array.Empty<object>());
                    typeof(JourneyGame).GetField("save", flags).SetValue(game, new JourneySave { chapter = chapter, step = step, unlocked = 26 });
                    typeof(JourneyGame).GetField("started", flags).SetValue(game, true);
                    call("CloseMenu", Array.Empty<object>());
                    call("LoadWorld", new object[] { false });
                    await Frames(4);
                    if (!game.PlayerRoot || !game.PlayerRoot.GetComponentInChildren<SpriteRenderer>(true).sprite) throw new Exception("Player sprite missing: " + chapter + "/" + step);
                    if (errors.Count > 0) throw new Exception(string.Join(";", errors.Distinct()));
                    checks++;
                }
                rows.Add("PASS chapter " + (chapter + 1) + ": " + book.chapters[chapter].steps.Length + " states");
                File.WriteAllLines(Root + "/playcheck-progress.txt", rows);
            }
            rows.Add("PASS " + checks + " chapter states; advancing frames; no runtime errors");
            game.ShowHome();
            await Frames(3);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Root + "/title-after.png"));
            await Frames(3);
            File.WriteAllLines(Root + "/playcheck.txt", rows);
        }
        catch (Exception e) { File.WriteAllText(Root + "/playcheck.txt", "FAIL " + e + "\n" + string.Join("\n", rows)); }
        finally
        {
            Application.logMessageReceived -= log;
            EditorApplication.isPlaying = false;
            running = false;
        }
    }
}
