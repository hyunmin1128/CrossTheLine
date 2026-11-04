using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFadeTransition : MonoBehaviour
{
    static SceneFadeTransition active;
    CanvasGroup group;

    public static void Load(string scene, float duration)
    {
        if (active) return;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Scene is missing from Build Settings: " + scene);
            return;
        }
        var root = new GameObject("SceneFadeTransition", typeof(Canvas), typeof(CanvasGroup), typeof(UnityEngine.UI.GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var black = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        black.transform.SetParent(root.transform, false);
        var rect = (RectTransform)black.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        black.GetComponent<UnityEngine.UI.Image>().color = Color.black;
        active = root.AddComponent<SceneFadeTransition>();
        active.group = root.GetComponent<CanvasGroup>();
        active.group.alpha = 0;
        active.StartCoroutine(active.Transition(scene, Mathf.Max(0.01f, duration)));
    }

    IEnumerator Transition(string scene, float duration)
    {
        yield return Fade(0, 1, duration);
        yield return SceneManager.LoadSceneAsync(scene);
        // Keep the first rendered frame of the new scene covered.
        yield return null;
        Debug.Log("[DesertJourney] Loaded " + scene + "; fading in.");
        yield return Fade(1, 0, duration);
        Debug.Log("[DesertJourney] Fade-in complete.");
        Destroy(gameObject);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
    void OnDestroy() { if (active == this) active = null; }
}
