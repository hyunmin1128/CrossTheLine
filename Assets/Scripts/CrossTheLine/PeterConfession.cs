using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace CrossTheLine
{
    public sealed class PeterConfession : MonoBehaviour
    {
        public enum Phase { Arrival, Reports, Question, Silence, Decision, Confessing, Joy, JoyReady, Blessing, Commission }
        public Phase CurrentPhase { get; private set; }
        public int Stage { get; private set; }
        public bool Confessed { get; private set; }
        public bool IsCinematic => CurrentPhase == Phase.Silence || CurrentPhase == Phase.Confessing || CurrentPhase == Phase.Joy || CurrentPhase == Phase.JoyReady || CurrentPhase == Phase.Blessing;
        public bool IsJoyful => Stage == 4;
        public Transform Player => player;
        public Sprite JoyPortrait => joyfulPortrait;
        public Vector3 Target => Stage == 0 ? new Vector3(-4.2f, -2.65f) : new Vector3(-.75f, -2.65f);

        JourneyGame game;
        Transform player, jesus;
        SpriteRenderer body, landscape;
        Material material;
        Sprite square, backgroundSprite, joyfulPortrait, normalPortrait;
        Transform[] disciples = new Transform[12];
        GameObject environment;
        Canvas cinematicCanvas;
        GameObject letterbox, thoughtPanel, confessionPanel, joyPanel;
        CanvasGroup joyFade;
        TextMeshProUGUI confessionQuote;
        UnityEngine.UI.Button blessingButton;
        UnityEngine.UI.Image dialoguePortrait;
        RectTransform portraitFrame, speakerRect, lineRect;
        Transform[] leaves = new Transform[24];
        Vector3 cameraTarget;
        float cameraSize, clock, breeze;
        bool busy, shutdown, oldPixel;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;
        readonly Vector3[] positions = {
            new Vector3(1.3f,-2.65f), new Vector3(2.7f,-2.4f), new Vector3(4.1f,-2.4f), new Vector3(5.5f,-2.4f),
            new Vector3(6.9f,-2.4f), new Vector3(8.3f,-2.4f), new Vector3(1.85f,-4.6f), new Vector3(3.2f,-4.6f),
            new Vector3(4.55f,-4.6f), new Vector3(5.9f,-4.6f), new Vector3(7.25f,-4.6f), new Vector3(8.6f,-4.6f)
        };

        public void Initialize(JourneyGame g, Transform p, SpriteRenderer b, Material m, Sprite s, int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game = g; player = p; body = b; material = m; square = s;
            backgroundSprite = JourneySpriteAssets.Load("CrossTheLine/CaesareaConfessionLandscape");
            joyfulPortrait = JourneySpriteAssets.Load("CrossTheLine/ConfessionJesusLaugh");
            normalPortrait = JourneySpriteAssets.Load("CrossTheLine/ConfessionJesusNormal");
            if (!backgroundSprite || !joyfulPortrait || !normalPortrait) throw new Exception("Confession landscape or existing portraits missing");
            pixel = g.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>(); if (pixel) { oldPixel = pixel.enabled; pixel.enabled = false; }
            BuildUI(); ConfigureStep(step);
        }
        Color C(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
        Transform Actor(string name, Sprite sprite, Vector3 point)
        {
            var root = JourneyObjectFactory.Create(name).transform; root.SetParent(environment.transform, false); root.position = point;
            var r = JourneyObjectFactory.Create("Sprite").GetOrAddComponent<SpriteRenderer>(); r.transform.SetParent(root, false); r.sprite = sprite; r.sharedMaterial = material; r.sortingOrder = 50;
            float scale = 1.85f / sprite.bounds.size.y; r.transform.localScale = Vector3.one * scale; r.transform.localPosition = new Vector3(JourneySpriteAssets.VisibleBounds(sprite).center.x * scale, -JourneySpriteAssets.VisibleBounds(sprite).min.y * scale); r.flipX = true;
            return root;
        }
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage = Mathf.Min(step, 5); clock = 0; busy = false; Confessed = Stage >= 4;
            if (environment) Destroy(environment); environment = JourneyObjectFactory.Create("CaesareaConfessionEnvironment"); environment.transform.SetParent(transform, false);
            var bg = JourneyObjectFactory.Create("두 큰 나무와 가이사랴 빌립보의 길").GetOrAddComponent<SpriteRenderer>(); bg.transform.SetParent(environment.transform, false); bg.sprite = backgroundSprite; bg.sharedMaterial = material; bg.sortingOrder = -30;
            bg.transform.localScale = Vector3.one * Mathf.Max(24 / backgroundSprite.bounds.size.x, 13.5f / backgroundSprite.bounds.size.y); landscape = bg;
            for (int i = 0; i < 12; i++)
            {
                disciples[i] = Actor(JourneyGame.ActorNames[i], game.disciples[i], Stage >= 4 && i == 0 ? Target : positions[i]);
                if (Stage == 3 && i == 0) disciples[i].gameObject.SetActive(false);
            }
            jesus = Stage == 3 ? Actor("질문하신 예수님", game.jesusFrames[0], new Vector3(-4.2f, -2.65f)) : null;
            if (jesus) jesus.GetChild(0).GetComponent<SpriteRenderer>().flipX = false;
            player.position = Stage == 0 ? new Vector3(-7.8f, -2.65f) : Stage == 3 ? positions[0] : new Vector3(-4.2f, -2.65f);
            player.rotation = Quaternion.identity; body.transform.localRotation = Quaternion.identity; body.maskInteraction = SpriteMaskInteraction.None; body.sortingOrder = 65; body.flipX = false;
            if (Stage != 3) foreach (var walker in body.GetComponents<DiscipleWalking>()) walker.enabled = false;
            for (int i = 0; i < 24; i++)
            {
                var o = JourneyObjectFactory.Create("바람에 흩날리는 잎 " + i); o.transform.SetParent(environment.transform, false); var r = o.GetOrAddComponent<SpriteRenderer>(); r.sprite = square; r.sharedMaterial = material; r.color = i % 3 == 0 ? C("c9b669") : C("71895a"); r.sortingOrder = i % 3 == 0 ? 72 : -5; o.transform.localScale = new Vector3(.055f, .018f, 1); leaves[i] = o.transform;
            }
            CurrentPhase = Stage == 0 ? Phase.Arrival : Stage == 1 ? Phase.Reports : Stage == 2 ? Phase.Question : Stage == 3 ? Phase.Decision : Stage == 4 ? Phase.Joy : Phase.Commission;
            cameraTarget = new Vector3(0, 0, -10); cameraSize = 5.625f;
            if (Stage == 4) { cameraTarget = new Vector3(-1.9f, -1.0f, -10); cameraSize = 3.9f; }
            if (Stage == 0) { game.journeyCamera.transform.position = cameraTarget; game.journeyCamera.orthographicSize = cameraSize; }
            game.BaptismCinema(IsCinematic); Hud(); SetPanels();
        }
        void Hud()
        {
            string[] goals = { "나무 아래 모인 제자들에게 다가가기", "사람들이 말하는 예수님에 대해 듣기", "너희는 나를 누구라 하느냐", "베드로 · 한 걸음 앞으로 나서 믿음 고백하기", "예수님의 기쁨과 축복", "고백에 담긴 새로운 부르심 기억하기" };
            game.ConfessionHud(goals[Stage], Stage == 0 ? "D / → · 제자들에게 다가가기    E · 질문하기" : Stage == 3 ? "A / ← · 앞으로 한 걸음    가까이에서 E · 고백하기" : "E · 이야기 진행");
        }
        public void Tick(float dt, bool canMove, bool press)
        {
            if (game.Mode == "menu" || game.Mode == "chapter_end") return;
            if (game.Mode == "dialogue") return;
            clock += dt; breeze += dt;
            for (int i = 0; i < leaves.Length; i++)
            {
                leaves[i].position = JourneyStageLayout.Point(this,leaves[i],"Placement 1",new Vector3(-12 + Mathf.Repeat(i * 1.13f + breeze * .24f, 24), 3.7f - Mathf.Repeat(i * .57f + breeze * .22f, 7.2f)));
                leaves[i].rotation = Quaternion.Euler(0, 0, Mathf.Sin(breeze + i) * 30);
            }
            if (CurrentPhase == Phase.Silence)
            {
                if (clock >= 2.3f) game.BaptismAdvance(); return;
            }
            if (CurrentPhase == Phase.Confessing)
            {
                confessionQuote.maxVisibleCharacters = Mathf.FloorToInt(clock * 22);
                if (clock >= 4.0f) { Confessed = true; game.BaptismAdvance(); } return;
            }
            if (CurrentPhase == Phase.Joy)
            {
                joyFade.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(clock / .7f));
                if (clock >= 3.2f) { CurrentPhase = Phase.JoyReady; blessingButton.interactable = true; } return;
            }
            if (CurrentPhase == Phase.JoyReady) { if (press) Interact(); return; }
            if (!canMove || busy) return;
            var k = Keyboard.current; float x = k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            if (Stage == 0 || Stage == 3)
            {
                var p = player.position; p.x = Mathf.Clamp(p.x + x * dt * (Stage == 3 ? 1.75f : 3.2f), Stage == 3 ? -.8f : -8, Stage == 3 ? 1.3f : -4.2f); player.position = p; game.MeetingWalk(x != 0, x);
            }
            if (press) Interact();
        }
        public void Interact()
        {
            if (busy || IsCinematic && CurrentPhase != Phase.JoyReady) return;
            if (Stage == 0 && player.position.x < -4.9f || Stage == 3 && player.position.x > -.4f) return;
            if (Stage == 3)
            {
                CurrentPhase = Phase.Confessing; clock = 0; cameraTarget = new Vector3(-1.9f, -1.0f, -10); cameraSize = 3.9f;
                foreach (var walker in body.GetComponents<DiscipleWalking>()) walker.enabled = false;
                body.flipX = true; confessionQuote.maxVisibleCharacters = 0; SetPanels(); game.BaptismCinema(true); return;
            }
            if (Stage == 4) CurrentPhase = Phase.Blessing;
            busy = true; game.MeetingTalk(() =>
            {
                busy = false;
                if (Stage == 2) { CurrentPhase = Phase.Silence; clock = 0; cameraTarget = new Vector3(-1.6f, -.6f, -10); cameraSize = 4.6f; SetPanels(); game.BaptismCinema(true); }
                else game.BaptismAdvance();
            });
        }
        public void DecorateDialogue(Transform dialogue, RectTransform speaker, RectTransform line)
        {
            speakerRect = speaker; lineRect = line;
            portraitFrame = Box(dialogue, "예수님 표정 테두리", new Vector2(.025f, .13f), new Vector2(.17f, .93f), C("d7bd75"));
            var image = Box(portraitFrame, "기존 예수님 표정", new Vector2(.035f, .04f), new Vector2(.965f, .96f), Color.white).GetComponent<UnityEngine.UI.Image>();
            image.sprite = Stage == 4 ? joyfulPortrait : normalPortrait; image.preserveAspect = true; dialoguePortrait = image;
            UpdatePortrait();
        }
        void UpdatePortrait()
        {
            if (!dialoguePortrait) return;
            bool show = game.DialogueSpeaker == "예수님"; portraitFrame.gameObject.SetActive(show);
            dialoguePortrait.sprite = Stage == 4 ? joyfulPortrait : normalPortrait;
            speakerRect.anchorMin = new Vector2(show ? .20f : .035f, .71f);
            lineRect.anchorMin = new Vector2(show ? .20f : .035f, .20f);
        }
        RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var o = JourneyObjectFactory.Create(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); o.transform.SetParent(parent, false);
            var r = o.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var image = o.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return r;
        }
        TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 min, Vector2 max, int size, Color color)
        {
            var o = JourneyObjectFactory.Create(name, typeof(RectTransform)); o.transform.SetParent(parent, false); var r = o.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = o.GetOrAddComponent<TextMeshProUGUI>(); t.font = game.koreanFont; t.text = value; t.fontSize = size; t.color = color; t.raycastTarget = false; return t;
        }
        void BuildUI()
        {
            var o = JourneyObjectFactory.Create("ConfessionCinemaUI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)); o.transform.SetParent(transform, false);
            cinematicCanvas = o.GetComponent<Canvas>(); cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay; cinematicCanvas.sortingOrder = 90;
            var scaler = o.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            letterbox = JourneyObjectFactory.Create("고백 순간의 화면 프레임", typeof(RectTransform)); letterbox.transform.SetParent(o.transform, false);
            var frame = letterbox.GetComponent<RectTransform>(); frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.offsetMin = frame.offsetMax = Vector2.zero;
            Box(letterbox.transform, "위쪽", new Vector2(0, .92f), Vector2.one, new Color(.06f, .10f, .09f, .8f));
            Box(letterbox.transform, "아래쪽", Vector2.zero, new Vector2(1, .045f), new Color(.06f, .10f, .09f, .8f));
            thoughtPanel = Box(o.transform, "베드로의 속마음", new Vector2(.05f, .61f), new Vector2(.44f, .81f), new Color(.12f, .19f, .17f, .93f)).gameObject;
            Text(thoughtPanel.transform, "이름", "베드로의 속마음", new Vector2(.06f, .69f), new Vector2(.94f, .92f), 18, C("dfc87f"));
            Text(thoughtPanel.transform, "믿음의 무게", "목숨이 위태로워져도…\n내가 믿는 분을 숨기지는 않겠다.", new Vector2(.06f, .15f), new Vector2(.94f, .65f), 23, C("f1e7c9"));
            confessionPanel = Box(o.transform, "목숨을 걸 각오의 고백", new Vector2(.30f, .66f), new Vector2(.95f, .89f), new Color(.08f, .15f, .13f, .9f)).gameObject;
            Text(confessionPanel.transform, "순간", "두려움을 넘어 · 베드로의 고백", new Vector2(.05f, .76f), new Vector2(.95f, .95f), 18, C("d9bf79"));
            confessionQuote = Text(confessionPanel.transform, "고백", "주는 그리스도시요,\n살아 계신 하나님의 아들이시니이다.", new Vector2(.05f, .12f), new Vector2(.95f, .74f), 34, C("f5ebce"));
            joyPanel = Box(o.transform, "예수님의 기쁜 응답", new Vector2(.05f, .37f), new Vector2(.39f, .84f), C("f0e5c6")).gameObject; joyFade = joyPanel.GetOrAddComponent<CanvasGroup>();
            var portrait = Box(joyPanel.transform, "사용자가 만든 기쁜 표정", new Vector2(.21f, .37f), new Vector2(.79f, .93f), Color.white).GetComponent<UnityEngine.UI.Image>(); portrait.sprite = joyfulPortrait; portrait.preserveAspect = true;
            Text(joyPanel.transform, "기쁨", "“네가 복이 있도다”", new Vector2(.08f, .20f), new Vector2(.92f, .35f), 28, C("365348")).alignment = TextAlignmentOptions.Center;
            var buttonRect = Box(joyPanel.transform, "축복 말씀 듣기", new Vector2(.10f, .05f), new Vector2(.90f, .17f), C("315e56"));
            buttonRect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true; blessingButton = buttonRect.gameObject.GetOrAddComponent<UnityEngine.UI.Button>(); blessingButton.targetGraphic = buttonRect.GetComponent<UnityEngine.UI.Image>(); blessingButton.onClick.AddListener(Interact);
            Text(buttonRect, "안내", "E · 축복 말씀 듣기", Vector2.zero, Vector2.one, 19, C("f5ebce")).alignment = TextAlignmentOptions.Center;
        }
        void SetPanels()
        {
            thoughtPanel.SetActive(CurrentPhase == Phase.Decision);
            confessionPanel.SetActive(CurrentPhase == Phase.Confessing || CurrentPhase == Phase.Silence);
            if (CurrentPhase == Phase.Silence) { confessionQuote.text = "“너희는 나를 누구라 하느냐”"; confessionQuote.maxVisibleCharacters = int.MaxValue; }
            else { confessionQuote.text = "주는 그리스도시요,\n살아 계신 하나님의 아들이시니이다."; }
            joyPanel.SetActive(CurrentPhase == Phase.Joy || CurrentPhase == Phase.JoyReady); joyFade.alpha = CurrentPhase == Phase.Joy ? 0 : 1;
            blessingButton.interactable = CurrentPhase == Phase.JoyReady;
            letterbox.SetActive(IsCinematic);
        }
        void LateUpdate()
        {
            if (shutdown || !game) return;
            cinematicCanvas.enabled = game.Mode != "menu" && game.Mode != "chapter_end";
            if (joyPanel) joyPanel.SetActive((CurrentPhase == Phase.Joy || CurrentPhase == Phase.JoyReady) && game.Mode != "dialogue");
            body.enabled = !(joyPanel.activeInHierarchy && cinematicCanvas.enabled);
            UpdatePortrait();
            string speaker = game.DialogueSpeaker;
            for (int i = 0; i < disciples.Length; i++)
            {
                var r = disciples[i].GetChild(0).GetComponent<SpriteRenderer>();
                bool dim = CurrentPhase == Phase.Confessing || CurrentPhase == Phase.Silence || !string.IsNullOrEmpty(speaker) && speaker != JourneyGame.ActorNames[i];
                r.color = dim ? new Color(.72f, .76f, .71f) : Color.white;
            }
            landscape.color = CurrentPhase == Phase.Silence || CurrentPhase == Phase.Decision ? new Color(.83f, .88f, .88f) : Color.white;
        }
        public void UpdateCamera()
        {
            if (game.Mode == "menu") return;
            float t = 1 - Mathf.Exp(-3 * Time.unscaledDeltaTime);
            game.journeyCamera.transform.position = Vector3.Lerp(game.journeyCamera.transform.position, cameraTarget, t);
            game.journeyCamera.orthographicSize = Mathf.Lerp(game.journeyCamera.orthographicSize, cameraSize, t);
        }
        public void Shutdown() { if (shutdown) return; shutdown = true; if (body) body.enabled = true; if (pixel) pixel.enabled = oldPixel; }
        void OnDestroy() { Shutdown(); }
    }
}
