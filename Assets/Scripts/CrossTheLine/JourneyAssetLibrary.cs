using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossTheLine
{
    // Scene references keep these assets in builds; physical folder names are irrelevant.
    [CreateAssetMenu(menuName = "CrossTheLine/Runtime Asset Library")]
    public sealed class JourneyAssetLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string key;
            public UnityEngine.Object[] assets;
        }
        public List<Entry> entries = new List<Entry>();
    }

    public static class JourneyAssets
    {
        static JourneyAssetLibrary library;
        static Dictionary<string, UnityEngine.Object[]> lookup;

        public static void Bind(JourneyAssetLibrary value)
        {
            if (!value) throw new InvalidOperationException("JourneyGame requires a runtime asset library.");
            if (library == value && lookup != null) return;
            library = value;
            lookup = value.entries.ToDictionary(e => e.key, e => e.assets ?? Array.Empty<UnityEngine.Object>(), StringComparer.Ordinal);
            JourneySpriteAssets.Reset();
        }

        public static T Load<T>(string key) where T : UnityEngine.Object => LoadAll<T>(key).FirstOrDefault();
        public static T[] LoadAll<T>(string key) where T : UnityEngine.Object
        {
            if (lookup == null) throw new InvalidOperationException("Bind JourneyGame.runtimeAssets before loading journey assets.");
            return lookup.TryGetValue(key, out var assets) ? assets.OfType<T>().Where(a => a).ToArray() : Array.Empty<T>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { library = null; lookup = null; JourneySpriteAssets.Reset(); }
    }
}
