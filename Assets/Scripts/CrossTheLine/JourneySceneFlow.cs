using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossTheLine
{
    public static class JourneySceneFlow
    {
        public const string Home = "CrossTheLineJourney";
        static readonly string[] scenes = { Home, "Chapter02_Baptism", "Chapter03_Temptation", "Chapter04_Disciples", "Chapter05_CanaWedding", "Chapter06_TempleCleansing", "Chapter07_SynagogueDispute", "Chapter08_MiraculousCatch", "Chapter09_MatthewCalling", "Chapter10_StormAtSea", "Chapter11_FeedingMultitude", "Chapter12_WaterWalking", "Chapter13_PeterConfession", "Chapter14_LordsPrayer", "Chapter15_ChildrenBlessing", "Chapter16_JerusalemEntry", "Chapter17_SecondTempleCleansing" };
        static string pending;
        static bool title;
        public static bool TestWithoutSaving;
        public static string SceneFor(int chapter) => chapter >= 0 && chapter < scenes.Length ? scenes[chapter] : Home;
        public static bool Route(int chapter, string progress)
        {
            if(Object.FindAnyObjectByType<JourneyContinuousWorld>())return false;
            var target = SceneFor(chapter);
            if (SceneManager.GetActiveScene().name == target) return false;
            pending = progress; title = false;
            SceneManager.LoadScene(target, LoadSceneMode.Single);
            return true;
        }
        public static void OpenHome() { pending = null; title = true; SceneManager.LoadScene(Home, LoadSceneMode.Single); }
        public static bool Take(out string progress, out bool home)
        {
            progress = pending; home = title; pending = null; title = false;
            return progress != null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { pending = null; title = false; TestWithoutSaving = false; }
    }
}
