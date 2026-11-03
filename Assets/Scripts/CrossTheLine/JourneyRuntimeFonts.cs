using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossTheLine
{
    // Shared font assets remain unchanged. System glyphs exist only for this play session.
    public static class JourneyRuntimeFonts
    {
        static readonly Dictionary<TMP_FontAsset, TMP_FontAsset> fonts = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
        static TMP_FontAsset korean;
        public static bool IsUsable(TMP_FontAsset font)
        {
            if (!font || !font.material) return false;
            var atlases = font.atlasTextures;
            int count = font.atlasTextureCount;
            if (atlases == null || count < 1 || count > atlases.Length) return false;
            for (int i = 0; i < count; i++) if (!atlases[i]) return false;
            return true;
        }
        public static TMP_FontAsset SystemKorean
        {
            get
            {
                // With domain reload disabled, the FontAsset can outlive its Play-mode atlas.
                if (!IsUsable(korean))
                {
                    if (korean) Dispose(korean);
                    korean = CreateSystemKorean();
                }
                return korean;
            }
        }

        public static TMP_FontAsset Resolve(TMP_FontAsset original)
        {
            if (!original || !Application.isPlaying) return original;
            foreach (var runtime in fonts.Values) if (runtime == original) return original;
            if (fonts.TryGetValue(original, out var cached) && IsUsable(cached)) return cached;
            korean = SystemKorean;
            if (!korean) return original;
            var copy = Object.Instantiate(original);
            copy.name = original.name + " (Runtime Korean)";
            copy.hideFlags = HideFlags.DontSave;
            var textures = new Texture2D[original.atlasTextures.Length];
            for (int i = 0; i < textures.Length; i++)
                if (original.atlasTextures[i])
                {
                    var source = original.atlasTextures[i];
                    textures[i] = new Texture2D(source.width, source.height, source.format, false)
                    { name = source.name + " (Runtime)", filterMode = source.filterMode, wrapMode = source.wrapMode, hideFlags = HideFlags.DontSave };
                    Graphics.CopyTexture(source, 0, 0, textures[i], 0, 0);
                }
            copy.atlasTextures = textures;
            copy.atlas = textures[0];
            copy.material = new Material(original.material) { hideFlags = HideFlags.DontSave };
            copy.material.mainTexture = textures[0];
            copy.fallbackFontAssetTable = new List<TMP_FontAsset> { korean };
            fonts[original] = copy;
            return copy;
        }

        static TMP_FontAsset CreateSystemKorean()
        {
            string[] families = { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR" };
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            foreach (var family in families)
            {
                if (!installed.Contains(family)) continue;
                var font = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);
                if (!font) continue;
                font.name = "Journey System Korean (" + family + ")";
                font.atlasPopulationMode = AtlasPopulationMode.DynamicOS;
                font.hideFlags = HideFlags.DontSave;
                font.material.hideFlags = HideFlags.DontSave;
                font.isMultiAtlasTexturesEnabled = true;
                return font;
            }
            Debug.LogWarning("No Korean system font was found. Install a Korean system font or assign your replacement TMP font.");
            return null;
        }

        public static void Apply(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    var font = Resolve(text.font);
                    if (font == text.font) continue;
                    text.font = font;
                    text.fontSharedMaterial = font.material;
                }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting -= Release;
            Application.quitting += Release;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Release()
        {
            ClearTemporaryFonts();
        }

        public static void ClearTemporaryFonts()
        {
            foreach (var font in fonts.Values)
                if (font) Dispose(font);
            fonts.Clear();
            if (korean)
            {
                Dispose(korean);
            }
            korean = null;
        }
        static void Dispose(Object value)
        {
            if(Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
