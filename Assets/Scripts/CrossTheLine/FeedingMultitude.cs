using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace CrossTheLine
{
    // The six visible groups are a window onto the multitude, not its total headcount.
    public sealed class FeedingMultitude : MonoBehaviour
    {
        public int Stage { get; private set; }
        public int SeatedGroups { get; private set; }
        public int FedGroups { get; private set; }
        public int LeftoverBaskets { get; private set; }
        public bool Prayed { get; private set; }
        public bool IsCinematic => praying || serving || gathering;
        public Transform Player => player;
        public Transform Basket => basket;
        public Vector3 Target => target;
        public int LoavesInBasket => basket ? basket.Find("음식").Cast<Transform>().Count(t => t.name.StartsWith("보리떡")) : 0;
        public int FishInBasket => basket ? basket.Find("음식").Cast<Transform>().Count(t => t.name.StartsWith("물고기")) : 0;

        JourneyGame game;
        Transform player, basket, child, jesus, flyingFood;
        Material material;
        Sprite square, basketSprite, breadSprite, fishSprite;
        Sprite[] seatedSprites;
        Sprite[] jesusActions;
        SpriteRenderer playerBody;
        GameObject environment;
        readonly Transform[] crowds = new Transform[6];
        readonly TextMeshPro[] groupLabels = new TextMeshPro[6];
        readonly bool[] seated = new bool[6], fed = new bool[6];
        readonly Vector3[] groupPoints = { new Vector3(-3, .2f), new Vector3(1, .2f), new Vector3(5, .2f), new Vector3(-3, -2.7f), new Vector3(1, -2.7f), new Vector3(5, -2.7f) };
        TextMeshPro basketLabel, headline;
        Canvas inventoryCanvas;
        GameObject inventoryPanel;
        TextMeshProUGUI inventoryText;
        Vector3 target, flightStart, flightEnd;
        float clock;
        int servingGroup;
        bool busy, praying, serving, gathering, seatingExplained, shutdown, oldPixel;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;

        public void Initialize(JourneyGame g, Transform p, SpriteRenderer body, Material m, Sprite s, int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game = g; player = p; playerBody = body; material = m; square = s;
            var sprites = JourneySpriteAssets.LoadAll("CrossTheLine/FeedingProps");
            basketSprite = sprites.FirstOrDefault(v => v.name == "OfferingBasket");
            breadSprite = sprites.FirstOrDefault(v => v.name == "BarleyLoaf");
            fishSprite = sprites.FirstOrDefault(v => v.name == "OfferingFish");
            if (!basketSprite || !breadSprite || !fishSprite) throw new Exception("Feeding props missing");
            seatedSprites = JourneySpriteAssets.LoadAll("CrossTheLine/FeedingSeatedCrowd").OrderBy(v => v.name).ToArray();
            if (seatedSprites.Length != 4) throw new Exception("Seated crowd sprites missing");
            var actions = JourneySpriteAssets.LoadAll("CrossTheLine/FeedingJesusActions");
            jesusActions = new[] { actions.FirstOrDefault(v => v.name == "JesusReceive"), actions.FirstOrDefault(v => v.name == "JesusThanksgiving"), actions.FirstOrDefault(v => v.name == "JesusShare") };
            if (jesusActions.Any(v => !v)) throw new Exception("Jesus feeding poses missing");
            BuildInventoryUI();
            pixel = game.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (pixel) { oldPixel = pixel.enabled; pixel.enabled = false; }
            ConfigureStep(step);
        }

        Color C(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
        Transform Shape(string name, Vector3 position, Vector2 size, string color, int order)
        {
            var o = JourneyObjectFactory.Create(name); o.transform.SetParent(environment.transform, false); o.transform.position = position;
            o.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = o.GetOrAddComponent<SpriteRenderer>(); r.sprite = square; r.sharedMaterial = material; r.color = C(color); r.sortingOrder = order;
            return o.transform;
        }
        Transform Prop(string name, Sprite sprite, Transform parent, Vector3 position, float width, int order)
        {
            var o = JourneyObjectFactory.Create(name); o.transform.SetParent(parent, false); o.transform.localPosition = position;
            o.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            var r = o.GetOrAddComponent<SpriteRenderer>(); r.sprite = sprite; r.sharedMaterial = material; r.sortingOrder = order;
            return o.transform;
        }
        Transform Actor(string name, Sprite sprite, Vector3 position, float height = 1.7f, int order = 45)
        {
            var root = JourneyObjectFactory.Create(name).transform; root.SetParent(environment.transform, false); root.position = position;
            var body = JourneyObjectFactory.Create("Sprite").transform; body.SetParent(root, false);
            var r = body.gameObject.GetOrAddComponent<SpriteRenderer>(); r.sprite = sprite; r.sharedMaterial = material; r.sortingOrder = order;
            float scale = height / sprite.bounds.size.y; body.localScale = Vector3.one * scale;
            body.localPosition = new Vector3(-sprite.bounds.center.x * scale, -sprite.bounds.min.y * scale, 0);
            int i = Array.IndexOf(game.disciples, sprite);
            if (i >= 0) body.gameObject.GetOrAddComponent<DiscipleWalking>().Initialize(i, root);
            return root;
        }
        TextMeshPro Label(string text, Vector3 position, float size = 2.5f, float width = 8)
        {
            var o = JourneyObjectFactory.Create(text); o.transform.SetParent(environment.transform, false); o.transform.position = position;
            var t = o.GetOrAddComponent<TextMeshPro>(); t.font = game.koreanFont; t.text = text; t.fontSize = size; t.color = C("344f43");
            t.alignment = TextAlignmentOptions.Center; t.GetComponent<RectTransform>().sizeDelta = new Vector2(width, .8f);
            t.GetComponent<MeshRenderer>().sortingOrder = 90; return t;
        }
        Transform MakeBasket(string name, Transform parent, Vector3 position, float width)
        {
            var root = JourneyObjectFactory.Create(name).transform; root.SetParent(parent, false); root.localPosition = position;
            Prop("손으로 짠 바구니", basketSprite, root, Vector3.zero, width, 60);
            var food = JourneyObjectFactory.Create("음식").transform; food.SetParent(root, false);
            for (int i = 0; i < 5; i++) Prop("보리떡 " + (i + 1), breadSprite, food, new Vector3((i % 3 - 1) * width * .22f, width * (.15f + i / 3 * .13f), 0), width * .24f, 62 + i / 3);
            for (int i = 0; i < 2; i++) Prop("물고기 " + (i + 1), fishSprite, food, new Vector3((i == 0 ? -.17f : .17f) * width, width * .05f, 0), width * .34f, 65);
            return root;
        }

        void BuildField()
        {
            Shape("빈 들의 풀밭", Vector3.zero, new Vector2(34, 20), "9fac72", -30);
            Shape("저녁 하늘", new Vector3(0, 6), new Vector2(34, 5), "d8dfbb", -29);
            Shape("먼 언덕", new Vector3(-4, 3.5f), new Vector2(24, 1.3f), "a6b983", -28);
            Shape("갈릴리 호수의 먼 물빛", new Vector3(8, 3.9f), new Vector2(11, .8f), "96bab4", -27);
            for (int i = 0; i < 140; i++)
            {
                float x = -10 + Mathf.Repeat(i * 1.731f, 21), y = -4.7f + Mathf.Repeat(i * .693f, 8);
                Shape("풀잎 " + i, new Vector3(x, y), new Vector2(.018f, .07f), i % 3 == 0 ? "d6ca8b" : "7f955e", -21);
            }
            for (int i = 0; i < 90; i++)
            {
                var a = Actor("멀리 이어진 무리 " + i, game.supportingCast[i % 3 == 0 ? 5 : i % 3 == 1 ? 6 : 2], new Vector3(-5.2f + i % 30 * .48f, 3.45f + i / 30 * .35f), .30f, -12 + i / 30);
                a.GetChild(0).GetComponent<SpriteRenderer>().flipX = true;
            }
            Shape("설교하는 바위", new Vector3(-7.1f, -.18f), new Vector2(2.25f, .35f), "8f9477", -5);
            Shape("바위 윗면", new Vector3(-7.1f, .02f), new Vector2(2.15f, .12f), "c1c1a0", -4);
            headline = Label("벳세다 · 빈 들에 모인 수많은 사람들", new Vector3(0, 3.55f), 3.0f, 19);
            for (int group = 0; group < 6; group++)
            {
                var root = JourneyObjectFactory.Create("50명 무리 " + (group + 1)).transform; root.SetParent(environment.transform, false); root.position = groupPoints[group]; crowds[group] = root;
                for (int i = 0; i < 50; i++)
                {
                    var a = Actor("사람 " + (i + 1), game.supportingCast[i % 4 == 0 ? 3 : i % 4 == 1 ? 2 : i % 4 == 2 ? 5 : 6], groupPoints[group] + new Vector3((i % 10 - 4.5f) * .32f, i / 10 * .53f, 0), .55f, 20 - i / 10);
                    a.SetParent(root, true);
                    a.GetChild(0).GetComponent<SpriteRenderer>().flipX = true;
                }
                groupLabels[group] = Label("무리 " + (group + 1), groupPoints[group] + new Vector3(0, -.5f), 2.0f, 3.7f);
            }
        }

        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage = Mathf.Min(step, 7); clock = 0; busy = praying = serving = gathering = false;
            seatingExplained = Stage >= 5;
            Prayed = Stage >= 4; SeatedGroups = Stage >= 5 ? 6 : 0; FedGroups = Stage >= 6 ? 6 : 0; LeftoverBaskets = 0;
            Array.Clear(seated, 0, 6); Array.Clear(fed, 0, 6);
            if (environment) Destroy(environment); environment = JourneyObjectFactory.Create("FeedingFieldEnvironment"); environment.transform.SetParent(transform, false);
            BuildField();
            for (int i = 0; i < 6; i++) { seated[i] = Stage >= 5; fed[i] = Stage >= 6; RefreshGroup(i); }
            child = Actor("도시락을 가져온 아이", game.supportingCast[3], new Vector3(-4.7f, -4.1f), 1.0f);
            jesus = Stage == 1 || Stage == 2 ? Actor("예수님", game.jesusFrames[0], new Vector3(-7.1f, .08f), 1.85f) : null;
            Actor("빌립", game.disciples[4], new Vector3(-8.4f, -2.6f));
            if (Stage != 1 && Stage != 2) Actor("안드레", game.disciples[1], new Vector3(-6.1f, -2.4f));
            player.position = Stage == 1 ? new Vector3(-7.5f, -4.1f) : Stage == 2 ? new Vector3(-3.3f, -4.7f) : new Vector3(-7.1f, .08f);
            basket = MakeBasket("줄지 않는 음식 바구니", environment.transform, Stage <= 1 ? new Vector3(-4.15f, -3.5f) : player.position + new Vector3(.65f, .67f), 1.25f);
            basketLabel = Label("보리떡 5개 · 물고기 2마리", basket.position + new Vector3(0, -.8f), 2.15f, 5);
            basketLabel.gameObject.SetActive(false);
            if (Stage == 0) { basket.gameObject.SetActive(false); basketLabel.gameObject.SetActive(false); child.gameObject.SetActive(false); }
            target = Stage == 1 ? child.position : Stage == 2 ? new Vector3(-7.1f, -.9f) : player.position;
            if (Stage == 7) { for (int i = 0; i < 12; i++) RevealLeftover(); headline.text = "모두 배불리 먹고도 · 열두 바구니가 남았습니다"; }
            if (Stage >= 3 && Stage <= 6) Pose(0);
            game.journeyCamera.orthographicSize = 5.625f; UpdateCamera(); game.BaptismCinema(false); Hud();
        }

        void RefreshGroup(int i)
        {
            groupLabels[i].text = fed[i] ? "50명 · 배불리 먹음" : seated[i] ? "50명 · 함께 앉음" : "무리 " + (i + 1);
            groupLabels[i].color = fed[i] ? C("f8ebae") : C("344f43");
            int person = 0;
            foreach (Transform a in crowds[i])
            {
                var body = a.GetChild(0); var renderer = body.GetComponent<SpriteRenderer>();
                if (seated[i])
                {
                    renderer.sprite = seatedSprites[person % 4]; renderer.flipX = false;
                    var sprite = renderer.sprite; float scale = Mathf.Min(.45f / sprite.bounds.size.y, .34f / sprite.bounds.size.x);
                    body.localScale = Vector3.one * scale;
                    body.localPosition = new Vector3(-sprite.bounds.center.x * scale, -sprite.bounds.min.y * scale, 0);
                }
                if (fed[i]) renderer.color = new Color(1, .96f, .85f);
                person++;
            }
            if (fed[i]) for (int k = 0; k < 10; k++) Prop("나눈 음식 " + k, k % 3 == 0 ? fishSprite : breadSprite, environment.transform, groupPoints[i] + new Vector3((k - 4.5f) * .32f, -.10f), .19f, 39);
        }
        void RevealLeftover()
        {
            basket.gameObject.SetActive(false); basketLabel.gameObject.SetActive(false);
            int i = LeftoverBaskets++;
            MakeBasket("남은 바구니 " + (i + 1), environment.transform, new Vector3(-5.5f + i % 6 * 2.2f, -.2f - i / 6 * 1.65f), 1.4f);
            Label((i + 1).ToString(), new Vector3(-5.5f + i % 6 * 2.2f, -.9f - i / 6 * 1.65f), 2.5f, 1.5f);
            if (headline) headline.text = "남은 음식을 모으니 · " + LeftoverBaskets + " / 12 바구니";
            foreach (var root in crowds) root.gameObject.SetActive(false);
            foreach (var label in groupLabels) label.gameObject.SetActive(false);
            foreach (Transform prop in environment.transform) if (prop.name.StartsWith("나눈 음식 ")) prop.gameObject.SetActive(false);
        }
        void Hud()
        {
            string[] goals = { "말씀을 듣고 배고파진 사람들", "안드레 · 아이에게 도시락 받기", "안드레 · 예수님께 음식 가져가기", "예수님 · 바구니를 들어 감사 기도", "사람들을 50명씩 앉히기 · " + SeatedGroups + "/6", "나눠도 줄지 않는 음식 · " + FedGroups + "/6 무리", "배불리 먹은 뒤 남은 음식 모으기", "열두 바구니에 담긴 기적 돌아보기" };
            string prompt = Stage == 4 && !seatingExplained ? "E · 사람들을 앉히라는 말씀 듣기" : Stage == 4 || Stage == 5 ? "WASD / 방향키 · 이동    무리 앞에서 E · " + (Stage == 4 ? "50명씩 앉히기" : "음식 나누기") : "E · 이야기 진행";
            if (Stage == 1 || Stage == 2) prompt = "WASD / 방향키 · 이동    가까이에서 E · 이야기";
            game.FeedingHud(goals[Stage], prompt);
        }

        public void Tick(float dt, bool canMove, bool press)
        {
            if (game.Mode == "menu" || game.Mode == "dialogue" || game.Mode == "chapter_end") return;
            if (praying)
            {
                clock += dt; basket.position = player.position + new Vector3(.02f, Mathf.Lerp(.8f, 2.28f, Mathf.Clamp01(clock)), 0);
                basketLabel.transform.position = basket.position + new Vector3(0, -.85f);
                headline.text = "예수님께서 바구니를 들어 하나님께 감사 기도하셨습니다";
                headline.transform.position = JourneyStageLayout.Point(this,headline.transform,"Placement 1",new Vector3(-5.5f, 3.55f));
                headline.fontSize = 2.2f;
                if (clock >= 3.5f) { Prayed = true; praying = false; game.BaptismAdvance(); } return;
            }
            if (serving)
            {
                clock += dt; float t = Mathf.Clamp01(clock / 1.5f);
                flyingFood.position = Vector3.Lerp(flightStart, flightEnd, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.5f;
                basket.localScale = Vector3.one * (1 + Mathf.Sin(t * Mathf.PI) * .10f);
                headline.text = "꺼내어 나누어도 · 바구니의 음식은 그대로입니다";
                if (t >= 1)
                {
                    Destroy(flyingFood.gameObject); flyingFood = null; serving = false; basket.localScale = Vector3.one;
                    fed[servingGroup] = true; FedGroups++; RefreshGroup(servingGroup); Pose(0); game.BaptismCinema(false); Hud();
                    if (FedGroups == 6) { busy = true; game.BaptismSay(new[] { "내레이션|예수님께서는 계속해서 떡과 물고기를 나누셨습니다. 눈앞의 무리 너머로도 음식이 전해졌고, 모든 사람이 원하는 만큼 배불리 먹었습니다." }, () => { busy = false; game.BaptismAdvance(); }); }
                } return;
            }
            if (gathering)
            {
                clock += dt; if (LeftoverBaskets < 12 && clock >= (LeftoverBaskets + 1) * .32f) RevealLeftover();
                if (clock >= 4.6f) { gathering = false; game.BaptismAdvance(); } return;
            }
            if (!canMove || busy) return;
            var keyboard = Keyboard.current; Vector2 input = Vector2.zero;
            if (keyboard != null) { input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0); input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0); }
            if (Stage == 1 || Stage == 2 || Stage == 4 || Stage == 5) { var p = player.position + (Vector3)(input.normalized * 3.4f * dt); p.x = Mathf.Clamp(p.x, -8.6f, 7.7f); p.y = Mathf.Clamp(p.y, -4.9f, 1.9f); player.position = p; game.MeetingWalk(input.sqrMagnitude > 0, input.x); }
            if ((Stage == 4 || Stage == 5) && input.sqrMagnitude == 0) Pose(0);
            if (Stage >= 2 && Stage < 7) { basket.position = player.position + new Vector3(.62f, .67f); basketLabel.transform.position = basket.position + new Vector3(0, -.83f); }
            if (press) Interact();
        }
        public void Interact()
        {
            if (busy || IsCinematic) return;
            if (Stage == 4 && !seatingExplained)
            {
                busy = true; game.MeetingTalk(() => { busy = false; seatingExplained = true; Hud(); }); return;
            }
            if (Stage == 4 || Stage == 5)
            {
                int closest = -1; float distance = 1.25f;
                for (int i = 0; i < 6; i++)
                {
                    if (Stage == 4 ? seated[i] : fed[i]) continue;
                    float d = Vector2.Distance(player.position, groupPoints[i] + Vector3.down * .5f);
                    if (d < distance) { distance = d; closest = i; }
                }
                if (closest < 0) return;
                if (Stage == 4)
                {
                    seated[closest] = true; SeatedGroups++; RefreshGroup(closest); Hud();
                    if (SeatedGroups == 6) { busy = true; game.BaptismSay(new[] { "내레이션|사람들은 푸른 풀밭에 오십 명씩 앉았습니다. 화면에 보이는 무리 너머로도 사람들이 이어져 있습니다." }, () => { busy = false; game.BaptismAdvance(); }); }
                }
                else
                {
                    servingGroup = closest; serving = true; clock = 0; Pose(2);
                    flightStart = basket.position + Vector3.up * .3f; flightEnd = groupPoints[closest] + new Vector3(0, .5f);
                    var portion = JourneyObjectFactory.Create("바구니에서 꺼낸 떡과 물고기").transform; portion.SetParent(environment.transform, false); portion.position = flightStart; flyingFood = portion;
                    Prop("보리떡", breadSprite, portion, new Vector3(-.18f, 0), .45f, 85);
                    Prop("물고기", fishSprite, portion, new Vector3(.20f, 0), .5f, 86);
                    game.BaptismCinema(true);
                } return;
            }
            if ((Stage == 1 || Stage == 2) && Vector2.Distance(player.position, target) > 1.7f) return;
            busy = true; game.MeetingTalk(() =>
            {
                busy = false;
                if (Stage == 3) { praying = true; clock = 0; Pose(1); game.BaptismCinema(true); }
                else if (Stage == 6) { gathering = true; clock = 0; game.BaptismCinema(true); }
                else game.BaptismAdvance();
            });
        }
        public void UpdateCamera()
        {
            game.journeyCamera.orthographicSize = praying ? 3.8f : 5.625f;
            game.journeyCamera.transform.position = praying ? new Vector3(-5.5f, 1, -10) : new Vector3(0, .1f, -10);
        }
        void Pose(int index)
        {
            playerBody.sprite = jesusActions[index]; playerBody.flipX = false;
            playerBody.transform.localScale = Vector3.one * ((index == 1 ? 2.02f : 1.85f) / jesusActions[index].bounds.size.y);
            var visible=JourneySpriteAssets.VisibleBounds(playerBody.sprite);float scale=playerBody.transform.localScale.x;
            playerBody.transform.localPosition = new Vector3(-visible.center.x*scale,-visible.min.y*scale,0);
        }
        void BuildInventoryUI()
        {
            var o = JourneyObjectFactory.Create("FeedingInventoryUI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            o.transform.SetParent(transform, false); inventoryCanvas = o.GetComponent<Canvas>(); inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay; inventoryCanvas.sortingOrder = 90;
            var scaler = o.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            inventoryPanel = JourneyObjectFactory.Create("줄지 않는 바구니 안내", typeof(RectTransform), typeof(UnityEngine.UI.Image)); inventoryPanel.transform.SetParent(o.transform, false);
            var rect = inventoryPanel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.76f, .075f); rect.anchorMax = new Vector2(.97f, .19f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = inventoryPanel.GetComponent<UnityEngine.UI.Image>(); image.color = C("e9dfb7"); image.raycastTarget = false;
            var text = JourneyObjectFactory.Create("바구니 속 음식 수", typeof(RectTransform)); text.transform.SetParent(inventoryPanel.transform, false);
            rect = text.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.05f, .08f); rect.anchorMax = new Vector2(.95f, .92f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            inventoryText = text.GetOrAddComponent<TextMeshProUGUI>(); inventoryText.font = game.koreanFont; inventoryText.fontSize = 18; inventoryText.color = C("344f43"); inventoryText.alignment = TextAlignmentOptions.Center; inventoryText.raycastTarget = false;
        }
        void LateUpdate()
        {
            if (!inventoryCanvas) return;
            inventoryCanvas.enabled = Stage >= 1 && Stage <= 5 && !praying && game.Mode != "menu" && game.Mode != "dialogue" && game.Mode != "chapter_end";
            inventoryText.text = (Stage == 5 ? "나눠도 남아 있는 음식" : "아이의 작은 도시락") + "\n보리떡 " + LoavesInBasket + "개 · 물고기 " + FishInBasket + "마리";
        }
        public void Shutdown() { if (shutdown) return; shutdown = true; if (pixel) pixel.enabled = oldPixel; }
        void OnDestroy() { Shutdown(); }
    }
}
