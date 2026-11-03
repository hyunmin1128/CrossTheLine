using TMPro;
using UnityEngine;

namespace CrossTheLine
{
    // Alpha is applied after Animator evaluation so existing sprite clips remain intact.
    [DefaultExecutionOrder(1000)]
    public sealed class NativityCutscene : MonoBehaviour
    {
        public Camera sceneCamera;
        public SpriteRenderer horse, angel, doctors;
        public TMP_Text subtitle;
        [Header("Editable scene captions (edit each TMP and RectTransform)")]
        public SpriteRenderer night;
        public TMP_Text oneDay, village, angelCaption, doctorsCaption;
        public TMP_Text sonOfGod, savior, famousHuman, jesusName, ageThirty, storyBegins;
        [Header("Jesus name reveal")]
        public TMP_Text hisNameIs;
        public RectTransform jesusMotion;
        public NativityNameFragments nameFragments;
        [Min(0f)] public float nameRevealDelay = .7f;
        [Min(0f)] public float fullNameHold = 1f;
        [Min(.1f)] public float nameMoveSeconds = 1.4f;
        [Min(1f)] public float nameZoom = 2.4f;
        public Vector2 nameFocusPosition = new Vector2(.5f, .5f);
        public Color nameNormalColor = Color.white;
        public Color nameFocusColor = new Color(.82f, .61f, .32f, 1f);
        [Min(0f)] public float nameFocusHold = 1.5f;
        [Min(.1f)] public float nameDissolveSeconds = 1.8f;
        [Min(0f)] public float nameScatterDistance = 140f;
        [Header("Caption timing (seconds)")]
        [Min(0f)] public float openingCaptionInterval = 2f;
        [Min(0f)] public float statementInterval = 2f;
        [Min(0f)] public float jesusNameDelay = 2f;
        [Min(0f)] public float finalCaptionInterval = 2f;
        [Header("Night layout (screen fractions)")]
        public Vector2 nightPosition = new Vector2(.5f, .55f);
        public Vector2 nightMaxSize = new Vector2(.60f, .64f);
        [Min(0.1f)] public float fadeSeconds = 1f;
        [Min(0f)] public float holdSeconds = 4f;
        [Min(0f)] public float openingDelay = 0.5f;
        [Min(0f)] public float sceneGap = 0.5f;
        [Min(0.1f)] public float captionTransition = 0.5f;
        [Header("Image layout (screen fractions)")]
        [Tooltip("화면 비율 좌표: X=왼쪽에서 오른쪽, Y=아래에서 위 (0~1)")]
        public Vector2 horsePosition = new Vector2(.5f, .55f);
        [Tooltip("화면 너비와 높이에서 차지할 최대 비율 (0~1)")]
        public Vector2 horseMaxSize = new Vector2(.60f, .64f);
        [Tooltip("화면 비율 좌표: X=왼쪽에서 오른쪽, Y=아래에서 위 (0~1)")]
        public Vector2 angelPosition = new Vector2(.28f, .70f);
        [Tooltip("화면 너비와 높이에서 차지할 최대 비율 (0~1)")]
        public Vector2 angelMaxSize = new Vector2(.38f, .38f);
        [Tooltip("화면 비율 좌표: X=왼쪽에서 오른쪽, Y=아래에서 위 (0~1)")]
        public Vector2 doctorsPosition = new Vector2(.72f, .36f);
        [Tooltip("화면 너비와 높이에서 차지할 최대 비율 (0~1)")]
        public Vector2 doctorsMaxSize = new Vector2(.38f, .36f);
        public float Elapsed { get; private set; }
        public bool IsComplete => Elapsed >= Duration;
        float Hold => Mathf.Max(0, holdSeconds);
        float Gap => Mathf.Max(0, sceneGap);
        float Transition => Mathf.Max(.01f, captionTransition);
        float NightEnd => Mathf.Max(0, openingDelay) + 2 * Fade + Mathf.Max(0, openingCaptionInterval) + Hold;
        float HorseStart => NightEnd + Gap;
        float AngelStart => HorseStart + 2 * Fade + Hold + Gap;
        float ChangeStart => AngelStart + Fade + Hold;
        float DoctorsStart => ChangeStart + Transition;
        float ImagesFadeStart => DoctorsStart + Fade + Hold;
        float StatementStart => ImagesFadeStart + Fade + Gap;
        float NameStart => StatementStart + 2 * Mathf.Max(0, statementInterval) + Mathf.Max(0, jesusNameDelay);
        float JesusRevealStart => NameStart + Fade + Mathf.Max(0, nameRevealDelay);
        float StatementFadeStart => JesusRevealStart + Fade + Mathf.Max(0, fullNameHold);
        float FocusStart => StatementFadeStart + Fade;
        float DissolveStart => FocusStart + Mathf.Max(.1f, nameMoveSeconds) + Mathf.Max(0, nameFocusHold);
        float FinalStart => DissolveStart + Mathf.Max(.1f, nameDissolveSeconds) + Gap;
        public float Duration => FinalStart + Mathf.Max(0, finalCaptionInterval) + Fade + Hold;
        float Fade => Mathf.Max(0.1f, fadeSeconds);
        bool playing;
        Vector2 horseSize, angelSize, doctorsSize;

        void Awake()
        {
            if (!sceneCamera || !horse || !angel || !doctors || !subtitle || !night ||
                !oneDay || !village || !angelCaption || !doctorsCaption || !sonOfGod ||
                !savior || !famousHuman || !jesusName || !ageThirty || !storyBegins ||
                !hisNameIs || !jesusMotion || !nameFragments)
            {
                Debug.LogError("NativityCutscene: scene references are missing.", this);
                enabled = false;
                return;
            }
            horseSize = horse.sprite.bounds.size;
            angelSize = angel.sprite.bounds.size;
            doctorsSize = doctors.sprite.bounds.size;
        }
        void OnEnable() { if (enabled && sceneCamera && horse && angel && doctors && subtitle && night) Replay(); }
        public void Replay() { Elapsed = 0; playing = true; Apply(); }
        public void PreviewAt(float seconds) { Elapsed = Mathf.Clamp(seconds, 0, Duration); playing = false; Apply(); }
        [ContextMenu("Preview/1 - Night captions")]
        void PreviewNight() => PreviewAt(NightEnd - Fade);
        [ContextMenu("Preview/2 - Horse caption")]
        void PreviewHorse() => PreviewAt(HorseStart + Fade);
        [ContextMenu("Preview/3 - Angel caption")]
        void PreviewAngel() => PreviewAt(AngelStart + Fade);
        [ContextMenu("Preview/4 - Wise men caption")]
        void PreviewDoctors() => PreviewAt(DoctorsStart + Fade);
        [ContextMenu("Preview/5 - Jesus introduction")]
        void PreviewIntroduction() => PreviewAt(NameStart + Fade);
        [ContextMenu("Preview/5a - Full name")]
        void PreviewFullName() => PreviewAt(JesusRevealStart + Fade);
        [ContextMenu("Preview/5b - Jesus in gold")]
        void PreviewNameFocus() => PreviewAt(DissolveStart - .1f);
        [ContextMenu("Preview/5c - Jesus fragments")]
        void PreviewNameFragments() => PreviewAt(DissolveStart + nameDissolveSeconds * .5f);
        [ContextMenu("Preview/6 - The story begins")]
        void PreviewEnding() => PreviewAt(Duration);
        [ContextMenu("Preview/Hide all")]
        void HidePreview() => PreviewAt(0);
        void LateUpdate()
        {
            if (playing) Elapsed = Mathf.Min(Duration, Elapsed + Time.unscaledDeltaTime);
            Apply();
        }
        static float Smooth(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
        static void Alpha(SpriteRenderer renderer, float alpha)
        {
            var color = renderer.color; color.a = alpha; renderer.color = color;
        }
        static void Caption(TMP_Text text, float alpha) { if (text) text.alpha = alpha; }
        float Reveal(float start) => Smooth((Elapsed - start) / Fade);
        float Window(float start, float fadeStart) => Reveal(start) * (1 - Reveal(fadeStart));
        void Place(SpriteRenderer renderer, Vector2 size, Vector2 viewport, float width, float height)
        {
            float worldHeight = 2 * sceneCamera.orthographicSize;
            float scale = Mathf.Min(worldHeight * sceneCamera.aspect * width / size.x, worldHeight * height / size.y);
            renderer.transform.localScale = Vector3.one * scale;
            var p = sceneCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 10));
            p.z = 0;
            renderer.transform.position = p;
        }
        void Apply()
        {
            // Read sprite bounds here as PreviewAt also runs outside Play mode.
            horseSize = horse.sprite.bounds.size;
            angelSize = angel.sprite.bounds.size;
            doctorsSize = doctors.sprite.bounds.size;
            Place(night, night.sprite.bounds.size, nightPosition, nightMaxSize.x, nightMaxSize.y);
            Place(horse, horseSize, horsePosition, horseMaxSize.x, horseMaxSize.y);
            Place(angel, angelSize, angelPosition, angelMaxSize.x, angelMaxSize.y);
            Place(doctors, doctorsSize, doctorsPosition, doctorsMaxSize.x, doctorsMaxSize.y);
            float nightAlpha = Window(Mathf.Max(0, openingDelay), NightEnd - Fade);
            Alpha(night, nightAlpha);
            Caption(oneDay, nightAlpha);
            Caption(village, Window(Mathf.Max(0, openingDelay) + Mathf.Max(0, openingCaptionInterval), NightEnd - Fade));
            float ha = Window(HorseStart, HorseStart + Fade + Hold);
            float aa = Window(AngelStart, ImagesFadeStart);
            float da = Window(DoctorsStart, ImagesFadeStart);
            Alpha(horse, ha); Alpha(angel, aa); Alpha(doctors, da);
            Caption(subtitle, ha);
            Caption(angelCaption, aa * (1 - Smooth((Elapsed - ChangeStart) / Transition)));
            Caption(doctorsCaption, da);
            Caption(sonOfGod, Window(StatementStart, StatementFadeStart));
            Caption(savior, Window(StatementStart + Mathf.Max(0, statementInterval), StatementFadeStart));
            Caption(famousHuman, Window(StatementStart + 2 * Mathf.Max(0, statementInterval), StatementFadeStart));
            Caption(hisNameIs, Window(NameStart, StatementFadeStart));
            ApplyNameReveal();
            Caption(ageThirty, Reveal(FinalStart));
            Caption(storyBegins, Reveal(FinalStart + Mathf.Max(0, finalCaptionInterval)));
        }
        void ApplyNameReveal()
        {
            if (!jesusMotion || !hisNameIs || !nameFragments) return;
            float focus = Smooth((Elapsed - FocusStart) / Mathf.Max(.1f, nameMoveSeconds));
            var origin = (RectTransform)jesusMotion.parent;
            var canvasRect = (RectTransform)origin.parent;
            Vector2 target = canvasRect.rect.min + Vector2.Scale(canvasRect.rect.size, nameFocusPosition);
            Vector3 worldTarget = canvasRect.TransformPoint(target);
            Vector3 localTarget = origin.InverseTransformPoint(worldTarget);
            jesusMotion.anchoredPosition = Vector2.Lerp(Vector2.zero, localTarget, focus);
            jesusMotion.localScale = Vector3.one * Mathf.Lerp(1, Mathf.Max(1, nameZoom), focus);
            Color tint = Color.Lerp(nameNormalColor, nameFocusColor, focus);
            float dissolve = Mathf.Clamp01((Elapsed - DissolveStart) / Mathf.Max(.1f, nameDissolveSeconds));
            jesusName.color = tint;
            Caption(jesusName, dissolve > 0 ? 0 : Reveal(JesusRevealStart));
            nameFragments.Show(jesusName, dissolve, tint, nameScatterDistance);
        }
    }
}
