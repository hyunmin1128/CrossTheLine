using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrossTheLine
{
    public sealed class LordsPrayer : MonoBehaviour
    {
        public enum Phase { Gathering, Request, Choosing, Revelation, Completed, Reflection }
        public static readonly string[] Lines = {
            "하늘에 계신 우리 아버지여", "이름이 거룩히 여김을 받으시오며", "나라이 임하옵시며",
            "뜻이 하늘에서 이룬 것 같이 땅에서도 이루어지이다", "오늘날 우리에게 일용할 양식을 주옵시고",
            "우리가 우리에게 죄 지은 자를 사하여 준 것 같이", "우리 죄를 사하여 주옵시고",
            "우리를 시험에 들게 하지 마옵시고", "다만 악에서 구하옵소서",
            "대개 나라와 권세와 영광이 아버지께 영원히 있사옵나이다", "아멘"
        };
        static readonly string[] Meanings = { "하나님을 아버지라 부르며 시작합니다.", "하나님의 이름이 거룩히 여김 받기를 구합니다.", "하나님의 나라를 구합니다.", "하나님의 뜻이 이 땅에서도 이루어지기를 구합니다.", "오늘 우리에게 필요한 양식을 구합니다.", "우리에게 잘못한 이를 용서하는 마음을 품습니다.", "우리의 죄를 용서해 주시기를 구합니다.", "시험에 빠지지 않도록 지켜 주시기를 구합니다.", "악에서 구해 주시기를 구합니다.", "나라와 권세와 영광을 하나님께 돌립니다.", "진심으로 이 기도에 함께 동의합니다." };
        public Phase CurrentPhase { get; private set; }
        public int Stage { get; private set; }
        public int CompletedLines { get; private set; }
        public int RevealedLines { get; private set; }
        public int Mistakes { get; private set; }
        public int[] OfferedLines { get; private set; } = Array.Empty<int>();
        public bool InGame => Stage == 2;
        public bool IsCinematic => Stage == 2;
        public bool NarrationActive => voice && (voice.isPlaying || CurrentPhase == Phase.Revelation);
        public AudioSource Narrator => voice;
        public Transform Player => player;
        public float RevealClock => clock;
        public float ShakeAmount { get; private set; }
        public string SkyText => skyText ? skyText.text : "";
        JourneyGame game;
        Transform player;
        SpriteRenderer body;
        GameObject environment, selectionPanel, skyPanel, conclusionPanel;
        Canvas canvas;
        TextMeshProUGUI skyText, activeLine, progress, meaning, feedback, conclusion;
        UnityEngine.UI.Button[] choices = new UnityEngine.UI.Button[3];
        TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[3];
        UnityEngine.UI.Button replay;
        UnityEngine.UI.Image skyGlow, flash;
        AudioSource voice, resonance;
        AudioClip[] narration = new AudioClip[11];
        AudioClip chord;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;
        bool oldPixel, shutdown, busy, voiceStarted, paused, replaying;
        float clock, duration, breeze;
        int revealIndex;
        Vector3 cameraBase;

        public void Initialize(JourneyGame g, Transform p, SpriteRenderer b, Material material, Sprite square, int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game = g; player = p; body = b;
            pixel = g.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (pixel) { oldPixel = pixel.enabled; pixel.enabled = false; }
            environment = JourneyObjectFactory.Create("MountOfOlivesPrayerEnvironment"); environment.transform.SetParent(transform, false);
            var landscape = JourneySpriteAssets.Load("CrossTheLine/OlivesPrayerLandscape");
            if (!landscape) throw new Exception("Mount of Olives prayer landscape missing");
            var bg = JourneyObjectFactory.Create("감람산의 올리브나무와 열린 하늘").GetOrAddComponent<SpriteRenderer>(); bg.transform.SetParent(environment.transform, false); bg.sprite = landscape; bg.sharedMaterial = material; bg.sortingOrder = -30;
            bg.transform.localScale = Vector3.one * Mathf.Max(24 / landscape.bounds.size.x, 13.5f / landscape.bounds.size.y);
            for (int i = 0; i < 12; i++)
            {
                var root = JourneyObjectFactory.Create(JourneyGame.ActorNames[i]).transform; root.SetParent(environment.transform, false);
                root.position = JourneyStageLayout.Point(this,root,"Placement 1",new Vector3(4.2f + i % 4 * 1.45f + (i / 4 % 2) * .35f, -2.65f - i / 4 * 1.5f));
                var r = JourneyObjectFactory.Create("Sprite").GetOrAddComponent<SpriteRenderer>(); r.transform.SetParent(root, false); r.sprite = g.disciples[i]; r.sharedMaterial = material; r.sortingOrder = 45 + i / 4;
                float scale = 1.45f / r.sprite.bounds.size.y; r.transform.localScale = Vector3.one * scale; r.transform.localPosition = new Vector3(-JourneySpriteAssets.VisibleBounds(r.sprite).center.x * scale, -JourneySpriteAssets.VisibleBounds(r.sprite).min.y * scale); r.flipX = true;
                if (i == 3) root.gameObject.SetActive(false); // John is the controlled disciple until the prayer begins.
            }
            var jesus = JourneyObjectFactory.Create("기도를 가르치시는 예수님").GetOrAddComponent<SpriteRenderer>(); jesus.transform.SetParent(environment.transform, false);
            var prayerPose = JourneySpriteAssets.LoadAll("CrossTheLine/FeedingJesusActions").FirstOrDefault(s => s.name == "JesusThanksgiving");
            jesus.sprite = prayerPose ? prayerPose : g.jesusFrames[0]; jesus.sharedMaterial = material; jesus.sortingOrder = 50;
            float js = 2.05f / jesus.sprite.bounds.size.y; jesus.transform.localScale = Vector3.one * js;
            jesus.transform.position = JourneyStageLayout.Point(this,jesus.transform,"Placement 2",new Vector3(4.8f - JourneySpriteAssets.VisibleBounds(jesus.sprite).center.x * js, -1.1f - JourneySpriteAssets.VisibleBounds(jesus.sprite).min.y * js));
            BuildUI(); BuildAudio(); ConfigureStep(step);
        }
        Color C(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage = Mathf.Min(step, 3); busy = false; clock = 0; replaying = false; voiceStarted = false;
            voice.Stop(); resonance.Stop(); CompletedLines = Stage >= 3 ? Lines.Length : 0; RevealedLines = 0; revealIndex = 0; ShakeAmount = 0;
            CurrentPhase = Stage == 0 ? Phase.Gathering : Stage == 1 ? Phase.Request : Stage == 2 ? Phase.Choosing : Phase.Reflection;
            player.position = Stage == 0 ? new Vector3(-4.7f, -4.5f) : new Vector3(3.1f, -2.9f);
            body.enabled = Stage < 2; body.flipX = false; body.maskInteraction = SpriteMaskInteraction.None; player.rotation = Quaternion.identity;
            environment.transform.Find("요한").gameObject.SetActive(Stage >= 2);
            cameraBase = new Vector3(0, -.25f, -10); game.journeyCamera.transform.position = cameraBase; game.journeyCamera.orthographicSize = 5.625f;
            UpdateSky(); canvas.enabled = true; conclusionPanel.SetActive(false); skyPanel.SetActive(Stage >= 2); selectionPanel.SetActive(Stage == 2);
            if (Stage == 2) RebuildChoices();
            game.BaptismCinema(Stage == 2); game.PrayerHud(Stage < 2 ? "예수님께 기도를 배워 달라고 부탁하기" : Stage == 2 ? "주기도문을 한 문장씩 완성하기" : "배운 기도를 마음에 담기", Stage == 0 ? "D / → · 예수님께 다가가기    E · 부탁하기" : "E · 이야기 진행");
        }
        public void Tick(float dt, bool canMove, bool press)
        {
            if (game.Mode == "menu" || game.Mode == "chapter_end" || game.Mode == "dialogue") return;
            breeze += dt;
            if (CurrentPhase == Phase.Revelation)
            {
                clock += dt;
                if (!voiceStarted && clock >= .15f) { voiceStarted = true; voice.clip = narration[0]; voice.Play(); }
                float strength = Mathf.Max(0, 1 - clock / .65f); ShakeAmount = strength * (game.GentleChallenge ? .025f : .065f);
                flash.color = new Color(1, .95f, .74f, strength * .14f);
                skyGlow.color = new Color(.98f, .84f, .47f, .08f + .13f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(clock / duration)));
                if (clock >= duration)
                {
                    ShakeAmount = 0; flash.color = Color.clear;
                    CurrentPhase = Phase.Completed; selectionPanel.SetActive(false); conclusionPanel.SetActive(true); conclusion.text = "함께 드린 기도가 하늘에 새겨졌습니다.\nE · 기도를 마음에 담기";
                }
                return;
            }
            if (CurrentPhase == Phase.Choosing)
            {
                var k = Keyboard.current;
                if (k != null) { if (k.digit1Key.wasPressedThisFrame) SelectLine(OfferedLines[0]); else if (k.digit2Key.wasPressedThisFrame) SelectLine(OfferedLines[1]); else if (k.digit3Key.wasPressedThisFrame && OfferedLines.Length > 2) SelectLine(OfferedLines[2]); }
                return;
            }
            if (CurrentPhase == Phase.Completed)
            {
                // Narration continues in the background; it never holds the player here.
                if (voiceStarted && !voice.isPlaying && revealIndex < Lines.Length - 1) { revealIndex++; voice.clip = narration[revealIndex]; voice.Play(); }
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ReplayLast(); else if (press) Interact(); return;
            }
            if (!canMove || busy) return;
            if (Stage == 0)
            {
                var k = Keyboard.current; float x = k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                var p = player.position; p.x = Mathf.Clamp(p.x + x * dt * 2.8f, -4.7f, 3.1f); p.y = Mathf.Lerp(-4.5f, -2.9f, Mathf.InverseLerp(-4.7f, 3.1f, p.x)); player.position = p; game.MeetingWalk(x != 0, x);
            }
            if (press) Interact();
        }
        public void Interact()
        {
            if (busy || game.Mode == "menu" || game.Mode == "dialogue") return;
            if (CurrentPhase == Phase.Completed) { game.BaptismAdvance(); return; }
            if (Stage == 2 || Stage == 0 && player.position.x < 2.5f) return;
            busy = true; game.MeetingTalk(() => { busy = false; game.BaptismAdvance(); });
        }
        void RebuildChoices()
        {
            int index = CompletedLines;
            meaning.text = Meanings[index]; feedback.text = "다음에 이어질 문장을 골라 주세요."; feedback.color = C("d9dcd0");
            // All choices are genuine lines of the same prayer, never competing translations.
            int[] candidates = { index, (index + 1) % Lines.Length, (index + 4) % Lines.Length };
            OfferedLines = candidates.OrderBy(i => (i * 7 + index * 3) % 13).ToArray();
            for (int i = 0; i < 3; i++) { choices[i].interactable = true; choiceLabels[i].text = (i + 1) + "  " + Lines[OfferedLines[i]]; }
            activeLine.text = "기도를 모두 완성하면\n하늘의 낭독이 시작됩니다."; activeLine.maxVisibleCharacters = int.MaxValue;
            replay.interactable = false; progress.text = "주기도문 완성하기  ·  " + CompletedLines + " / " + Lines.Length;
        }
        public void SelectLine(int index)
        {
            if (shutdown || Stage != 2 || CurrentPhase != Phase.Choosing || game.Mode == "menu" || !OfferedLines.Contains(index)) return;
            if (index != CompletedLines) { Mistakes++; feedback.text = "순서를 다시 생각해 보세요. " + Meanings[CompletedLines]; feedback.color = C("ebc58c"); return; }
            CompletedLines++;
            if (CompletedLines < Lines.Length) { UpdateSky(); RebuildChoices(); }
            else StartReveal();
        }
        public void ReplayLast()
        {
            if (CompletedLines != Lines.Length || CurrentPhase != Phase.Completed || game.Mode == "menu") return;
            revealIndex = 0; voiceStarted = true; voice.Stop(); voice.clip = narration[0]; voice.Play();
        }
        void StartReveal()
        {
            CurrentPhase = Phase.Revelation; revealIndex = 0; clock = 0; voiceStarted = false; duration = .8f;
            RevealedLines = Lines.Length; UpdateSky();
            selectionPanel.SetActive(false); activeLine.text = "주기도문이\n완성되었습니다.\n\nE · 기도를 마음에 담기"; activeLine.maxVisibleCharacters = int.MaxValue; progress.text = "주기도문  ·  " + Lines.Length + " / " + Lines.Length;
            resonance.Stop(); resonance.PlayOneShot(chord, .52f); replay.interactable = false;
        }
        void UpdateSky(int visible = -1)
        {
            if (visible < 0) visible = CompletedLines;
            skyText.text = string.Join("\n", Lines.Take(visible));
        }
        void LateUpdate()
        {
            if (shutdown || !game) return;
            bool pauseNow = game.Mode == "menu" || game.Mode == "chapter_end";
            if (pauseNow != paused) { paused = pauseNow; if (paused) { voice.Pause(); resonance.Pause(); } else { voice.UnPause(); resonance.UnPause(); } }
            voice.mute = resonance.mute = game.SoundMuted;
            canvas.enabled = !pauseNow && game.Mode != "dialogue" && Stage == 2;
            selectionPanel.SetActive(Stage == 2 && CurrentPhase == Phase.Choosing);
            replay.gameObject.SetActive(Stage == 2 && CurrentPhase == Phase.Completed); replay.interactable = CurrentPhase == Phase.Completed;
        }
        public void UpdateCamera()
        {
            if (game.Mode == "menu") return;
            var offset = CurrentPhase == Phase.Revelation ? new Vector3(Mathf.Sin(clock * 37), Mathf.Sin(clock * 43) * .65f) * ShakeAmount : Vector3.zero;
            game.journeyCamera.transform.position = cameraBase + offset;
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
            var t = o.GetOrAddComponent<TextMeshProUGUI>(); t.font = game.koreanFont; t.text = value; t.fontSize = size; t.color = color; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        UnityEngine.UI.Button Button(Transform parent, string name, Vector2 min, Vector2 max, Action action, out TextMeshProUGUI label)
        {
            var rect = Box(parent, name, min, max, C("f2e8c9")); rect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var b = rect.gameObject.GetOrAddComponent<UnityEngine.UI.Button>(); b.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>(); b.onClick.AddListener(() => action());
            label = Text(rect, "문장", "", new Vector2(.035f, .08f), new Vector2(.965f, .92f), 20, C("263c3a")); label.alignment = TextAlignmentOptions.MidlineLeft; return b;
        }
        void BuildUI()
        {
            var root = JourneyObjectFactory.Create("PrayerSkyUI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)); root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 95;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            flash = Box(root.transform, "하늘의 울림", Vector2.zero, Vector2.one, Color.clear).GetComponent<UnityEngine.UI.Image>();
            skyGlow = Box(root.transform, "하늘빛", new Vector2(.02f, .35f), new Vector2(.64f, .98f), new Color(.98f, .84f, .47f, .08f)).GetComponent<UnityEngine.UI.Image>();
            skyPanel = Box(root.transform, "하늘에 새겨지는 기도", new Vector2(.03f, .34f), new Vector2(.65f, .97f), new Color(.055f, .14f, .18f, .60f)).gameObject;
            Text(skyPanel.transform, "장소", "감람산  /  함께 배우는 기도", new Vector2(.045f, .91f), new Vector2(.955f, .98f), 18, C("ccbb80"));
            skyText = Text(skyPanel.transform, "완성된 주기도문", "", new Vector2(.045f, .03f), new Vector2(.955f, .89f), 23, C("f6de98")); skyText.lineSpacing = 8;
            var banner = Box(root.transform, "지금 하늘에 쓰이는 문장", new Vector2(.68f, .67f), new Vector2(.975f, .96f), new Color(.06f, .15f, .19f, .78f));
            progress = Text(banner, "진행", "", new Vector2(.065f, .80f), new Vector2(.935f, .95f), 19, C("d4bd7c"));
            activeLine = Text(banner, "낭독 문장", "", new Vector2(.065f, .12f), new Vector2(.935f, .76f), 32, C("ffe7a8")); activeLine.alignment = TextAlignmentOptions.MidlineLeft;
            selectionPanel = Box(root.transform, "기도 문장 선택", new Vector2(.03f, .025f), new Vector2(.65f, .325f), new Color(.055f, .14f, .18f, .96f)).gameObject;
            meaning = Text(selectionPanel.transform, "기도의 의미", "", new Vector2(.035f, .81f), new Vector2(.965f, .98f), 18, C("ead59d"));
            feedback = Text(selectionPanel.transform, "안내", "", new Vector2(.035f, .66f), new Vector2(.965f, .81f), 14, C("d9dcd0"));
            for (int i = 0; i < 3; i++) { int slot = i; float top = .64f - i * .21f; choices[i] = Button(selectionPanel.transform, "선택 " + (i + 1), new Vector2(.035f, top - .19f), new Vector2(.965f, top), () => SelectLine(OfferedLines[slot]), out choiceLabels[i]); }
            replay = Button(root.transform, "마지막 문장 다시 듣기", new Vector2(.69f, .58f), new Vector2(.97f, .65f), ReplayLast, out var replayText); replayText.text = "R · 완성된 기도 다시 듣기"; replayText.fontSize = 16;
            conclusionPanel = Box(root.transform, "완성 안내", new Vector2(.03f, .09f), new Vector2(.65f, .28f), new Color(.055f, .14f, .18f, .95f)).gameObject;
            conclusion = Text(conclusionPanel.transform, "마음에 담기", "", new Vector2(.05f, .10f), new Vector2(.95f, .90f), 26, C("f6de98")); conclusion.alignment = TextAlignmentOptions.Center;
        }
        void BuildAudio()
        {
            voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.volume = .85f; voice.spatialBlend = 0;
            resonance = gameObject.AddComponent<AudioSource>(); resonance.playOnAwake = false; resonance.volume = .55f; resonance.spatialBlend = 0;
            var reverb = gameObject.GetOrAddComponent<AudioReverbFilter>(); reverb.reverbPreset = AudioReverbPreset.User; reverb.decayTime = 2.3f; reverb.reverbLevel = -1700; reverb.reflectionsLevel = -2400; reverb.dryLevel = 0;
            for (int i = 0; i < Lines.Length; i++) { narration[i] = JourneyAssets.Load<AudioClip>("CrossTheLine/PrayerAudio/PrayerLine" + i.ToString("00")); if (!narration[i] || narration[i].length < .1f) throw new Exception("Prayer narration missing: " + i); }
            const int rate = 22050; var samples = new float[rate * 5]; var random = new System.Random(1401); float low = 0;
            float[] notes = { 130.81f, 196f, 261.63f, 329.63f, 392f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate; low = low * .992f + ((float)random.NextDouble() * 2 - 1) * .008f;
                float rumble = (low * 1.8f + Mathf.Sin(2 * Mathf.PI * 44 * t) * .12f) * Mathf.Exp(-t * 2.4f);
                float pad = 0; foreach (float f in notes) pad += Mathf.Sin(2 * Mathf.PI * f * t + Mathf.Sin(t * .8f) * .15f) * .035f;
                float envelope = Mathf.Min(t * 5, 1) * Mathf.Exp(-t * .62f) * Mathf.Clamp01(5 - t);
                samples[i] = Mathf.Clamp((rumble + pad) * envelope, -.8f, .8f);
            }
            chord = AudioClip.Create("PrayerHeavenResonance_OriginalSynth", samples.Length, 1, rate, false); chord.SetData(samples, 0);
        }
        public void Shutdown() { if (shutdown) return; shutdown = true; if (voice) voice.Stop(); if (resonance) resonance.Stop(); if (body) body.enabled = true; if (pixel) pixel.enabled = oldPixel; if (chord) Destroy(chord); }
        void OnDestroy() { Shutdown(); }
    }
}

