using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace CrossTheLine
{
    public sealed class WaterWalking : MonoBehaviour
    {
        public enum Phase { Crowd, Departure, Mountain, Prayer, Approach, Reassure, Invitation, StepOut, Walking, Sinking, Rescuing, Rescued, Returning, Calming, Quiet }
        public Phase CurrentPhase { get; private set; }
        public int Stage { get; private set; }
        public bool IsCinematic => CurrentPhase == Phase.Departure || CurrentPhase == Phase.Prayer || CurrentPhase == Phase.StepOut || CurrentPhase == Phase.Sinking || CurrentPhase == Phase.Rescuing || CurrentPhase == Phase.Returning || CurrentPhase == Phase.Calming;
        public bool InGame => CurrentPhase == Phase.Walking;
        public bool Prayed { get; private set; }
        public bool PeterRescued { get; private set; }
        public bool BothAboard { get; private set; }
        public float StormStrength { get; private set; }
        public float SinkDepth { get; private set; }
        public float WaterWalkDistance { get; private set; }
        public bool HandsJoined { get; private set; }
        public Vector3 JesusGrip => Grip(body, new Vector2(.10f, .49f));
        public Vector3 PeterGrip => peter ? Grip(peter.GetChild(0).GetComponent<SpriteRenderer>(), new Vector2(.90f, .81f)) : Vector3.zero;
        public Transform Player => player;
        public Transform Boat => boat;
        public Transform Peter => peter;
        public Transform Jesus => Stage == 2 || Stage == 3 || Stage >= 6 ? player : jesus;
        public const float SurfaceY = -1.4f;

        JourneyGame game;
        Transform player, boat, peter, jesus, ripple, waterMask;
        SpriteRenderer body, sky, sea;
        Material material;
        Sprite square, hullSprite, sinkingSprite, rescueSprite, prayerSprite;
        Sprite[] waveSprites;
        GameObject environment;
        Transform[] waves = new Transform[12], rain = new Transform[28], disciples = new Transform[6];
        TextMeshPro caption;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> ownedMaterials = new List<Material>();
        float clock, weatherClock;
        Vector3 rescueStart, rescueContact, rescueBodyRest, rescueBodyReach;
        bool busy, shutdown, oldPixel;
        UnityEngine.Rendering.Universal.PixelPerfectCamera pixel;

        public void Initialize(JourneyGame g, Transform p, SpriteRenderer b, Material m, Sprite s, int step)
        {JourneyObjectFactory.BeginStage(this,step);
            game = g; player = p; body = b; material = m; square = s;
            hullSprite = JourneySpriteAssets.LoadAll("CrossTheLine/GalileeBoatHull").FirstOrDefault();
            waveSprites = JourneySpriteAssets.LoadAll("CrossTheLine/SeaWaveAtlas").OrderBy(v => v.name).ToArray();
            var poses = JourneySpriteAssets.LoadAll("CrossTheLine/WaterRescueActions");
            sinkingSprite = poses.FirstOrDefault(v => v.name == "PeterSinking"); rescueSprite = poses.FirstOrDefault(v => v.name == "JesusRescuing");
            prayerSprite = JourneySpriteAssets.LoadAll("CrossTheLine/FeedingJesusActions").FirstOrDefault(v => v.name == "JesusThanksgiving");
            if (!hullSprite || waveSprites.Length != 3 || !sinkingSprite || !rescueSprite || !prayerSprite) throw new Exception("Water walking art missing");
            pixel = g.journeyCamera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (pixel) { oldPixel = pixel.enabled; pixel.enabled = false; }
            ConfigureStep(step);
        }
        Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        Transform Shape(string name, Transform parent, Vector3 position, Vector2 size, string color, int order)
        {
            var o = JourneyObjectFactory.Create(name); o.transform.SetParent(parent, false); o.transform.localPosition = position; o.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = o.GetOrAddComponent<SpriteRenderer>(); r.sprite = square; r.sharedMaterial = material; r.color = C(color); r.sortingOrder = order; return o.transform;
        }
        Transform Actor(string name, Sprite sprite, Transform parent, Vector3 position, float height = 1.6f, bool walking = false)
        {
            var root = JourneyObjectFactory.Create(name).transform; root.SetParent(parent, false); root.localPosition = position;
            var r = JourneyObjectFactory.Create("Sprite").GetOrAddComponent<SpriteRenderer>(); r.transform.SetParent(root, false); r.sharedMaterial = material; r.sortingOrder = 50;
            SetBody(r, sprite, height);
            int index = Array.IndexOf(game.disciples, sprite); if (walking && index >= 0) r.gameObject.GetOrAddComponent<DiscipleWalking>().Initialize(index, root);
            return root;
        }
        void SetBody(SpriteRenderer renderer, Sprite sprite, float height)
        {
            renderer.sprite = sprite; renderer.flipX = false; renderer.transform.localRotation = Quaternion.identity;
            float scale = height / sprite.bounds.size.y; renderer.transform.localScale = Vector3.one * scale;
            renderer.transform.localPosition = new Vector3(-JourneySpriteAssets.VisibleBounds(sprite).center.x * scale, -JourneySpriteAssets.VisibleBounds(sprite).min.y * scale, 0);
        }
        // Palm centers in the two existing sprites; account for sprite pivots and mirroring.
        Vector3 Grip(SpriteRenderer renderer, Vector2 uv)
        {
            var bounds = renderer.sprite.bounds;
            var point = new Vector3(bounds.min.x + bounds.size.x * uv.x, bounds.min.y + bounds.size.y * uv.y);
            if (renderer.flipX) point.x = -point.x;
            if (renderer.flipY) point.y = -point.y;
            return renderer.transform.TransformPoint(point);
        }
        void PrepareRescue()
        {
            rescueStart = player.position;
            rescueBodyRest = body.transform.localPosition;
            var difference = PeterGrip - JesusGrip;
            rescueContact = rescueStart + new Vector3(difference.x, 0);
            rescueBodyReach = rescueBodyRest + new Vector3(0, difference.y);
        }
        void AnimateRescue(float time)
        {
            float approach = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .65f));
            player.position = Vector3.Lerp(rescueStart, rescueContact, approach);
            body.transform.localPosition = Vector3.Lerp(rescueBodyRest, rescueBodyReach, approach);
            if (time < .65f) return;
            HandsJoined = true;
            float lift = Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - .65f) / 1.65f));
            body.transform.localPosition = Vector3.Lerp(rescueBodyReach, rescueBodyRest, lift);
            peter.rotation = Quaternion.Euler(0, 0, lift * 32);
            // Constrain the palms together throughout the pull, rather than moving the bodies independently.
            peter.position += JesusGrip - PeterGrip;
            if (ripple) ripple.position = JourneyStageLayout.Point(this,ripple,"Placement 1",new Vector3(peter.position.x, SurfaceY));
        }
        TextMeshPro Label(string text, Vector3 position, float fontSize = 3, float width = 19)
        {
            var o = JourneyObjectFactory.Create(text); o.transform.SetParent(environment.transform, false); o.transform.position = position;
            var t = o.GetOrAddComponent<TextMeshPro>(); t.font = game.koreanFont; t.text = text; t.fontSize = fontSize; t.color = C("eee4bf"); t.alignment = TextAlignmentOptions.Center;
            t.GetComponent<RectTransform>().sizeDelta = new Vector2(width, .85f); t.GetComponent<MeshRenderer>().sortingOrder = 90; return t;
        }
        void Polygon(string name, Vector3[] vertices, string color, int order)
        {
            var o = JourneyObjectFactory.Create(name, typeof(MeshFilter), typeof(MeshRenderer)); o.transform.SetParent(environment.transform, false);
            var mesh = new Mesh(); mesh.vertices = vertices; mesh.triangles = new[] { 0, 1, 2 }; mesh.RecalculateBounds(); meshes.Add(mesh); o.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mat = new Material(JourneyAssets.Load<Material>("CrossTheLine/SailCanvas")); mat.SetColor("_BaseColor", C(color)); ownedMaterials.Add(mat); var r = o.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.sortingOrder = order;
        }
        void BuildBoat()
        {
            boat = JourneyObjectFactory.Create("제자들을 태운 배").transform; boat.SetParent(environment.transform, false); boat.position = JourneyStageLayout.Point(this,boat,"Placement 2",new Vector3(5, .1f));
            var hull = JourneyObjectFactory.Create("곡선 나무 선체").GetOrAddComponent<SpriteRenderer>(); hull.transform.SetParent(boat, false); hull.sprite = hullSprite; hull.sharedMaterial = material; hull.sortingOrder = 35; hull.transform.localScale = Vector3.one * (7.2f / hullSprite.bounds.size.x);
            Shape("갑판", boat, new Vector3(0, .02f), new Vector2(6.0f, .06f), "b49364", 55);
            Shape("돛대", boat, new Vector3(1.8f, 1.65f), new Vector2(.10f, 3.3f), "77614c", 32);
            var sailObject = JourneyObjectFactory.Create("밤바람에 흔들리는 돛", typeof(MeshFilter), typeof(MeshRenderer)); sailObject.transform.SetParent(boat, false);
            var sailMesh = new Mesh(); sailMesh.vertices = new[] { new Vector3(1.85f, 3.2f), new Vector3(3, 2.85f), new Vector3(3.2f, 1.0f), new Vector3(1.85f, .9f) }; sailMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; sailMesh.RecalculateBounds(); meshes.Add(sailMesh);
            sailObject.GetComponent<MeshFilter>().sharedMesh = sailMesh; sailObject.GetComponent<MeshRenderer>().sharedMaterial = JourneyAssets.Load<Material>("CrossTheLine/SailCanvas"); sailObject.GetComponent<MeshRenderer>().sortingOrder = 31;
            Rope("뱃머리 밧줄", boat, new[] { new Vector3(1.8f, 3.3f), new Vector3(3.3f, .04f) }, .025f, "998e73", 33);
            Rope("후미 밧줄", boat, new[] { new Vector3(1.8f, 3.3f), new Vector3(-3.3f, .04f) }, .025f, "998e73", 33);
            int[] cast = { 0, 1, 2, 3, 4, 6 };
            for (int i = 0; i < 6; i++) disciples[i] = Actor(JourneyGame.ActorNames[cast[i]], game.disciples[cast[i]], boat, new Vector3(-2.65f + i * 1.05f, .07f), 1.25f);
        }
        void Rope(string name, Transform parent, Vector3[] points, float width, string color, int order, bool loop = false)
        {
            var o = JourneyObjectFactory.Create(name); o.transform.SetParent(parent, false); var line = o.GetOrAddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = loop; line.positionCount = points.Length; line.SetPositions(points);
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = C(color); line.sharedMaterial = material; line.sortingOrder = order;
        }
        void BuildRipple()
        {
            ripple = JourneyObjectFactory.Create("물 위 발밑 파문").transform; ripple.SetParent(environment.transform, false);
            var points = new Vector3[20]; for (int i = 0; i < points.Length; i++) { float angle = i * Mathf.PI * 2 / points.Length; points[i] = new Vector3(Mathf.Cos(angle) * .5f, Mathf.Sin(angle) * .09f); }
            Rope("발밑 하얀 물결", ripple, points, .035f, "b9d7df", 54, true);
        }
        Transform MakeWaterMask(SpriteRenderer victim)
        {
            var o = JourneyObjectFactory.Create("베드로를 덮는 수면", typeof(SpriteMask)); o.transform.SetParent(environment.transform, false);
            o.transform.position = JourneyStageLayout.Point(this,o.transform,"Placement 3",new Vector3(.6f, SurfaceY - 2)); o.transform.localScale = new Vector3(4, 4, 1);
            var mask = o.GetComponent<SpriteMask>(); mask.sprite = square; mask.isCustomRangeActive = true; mask.backSortingOrder = 49; mask.frontSortingOrder = 52;
            victim.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask; return o.transform;
        }
        void BuildSea()
        {
            sea = Shape("밤바다", environment.transform, new Vector3(0, -2), new Vector2(40, 17), "244c64", -30).GetComponent<SpriteRenderer>();
            sky = Shape("밤하늘", environment.transform, new Vector3(0, 5), new Vector2(40, 7), "253648", -29).GetComponent<SpriteRenderer>();
            for (int i = 0; i < 32; i++) Shape("별 " + i, environment.transform, new Vector3(-10 + Mathf.Repeat(i * 1.73f, 21), 2.1f + Mathf.Repeat(i * .47f, 3)), new Vector2(.03f, .03f), "9dacb6", -28);
            for (int i = 0; i < 12; i++) { var r = JourneyObjectFactory.Create("포말 파도 " + i).GetOrAddComponent<SpriteRenderer>(); r.transform.SetParent(environment.transform, false); r.sprite = waveSprites[2]; r.sharedMaterial = material; r.sortingOrder = i < 4 ? 20 : i < 8 ? 60 : 61; waves[i] = r.transform; }
            for (int i = 0; i < 28; i++) rain[i] = Shape("비바람 " + i, environment.transform, Vector3.zero, new Vector2(.018f, .42f), "96b3c1", 68);
            BuildBoat(); BuildRipple(); caption = Label("밤바다 · 바람을 거슬러 나아가는 제자들", new Vector3(0, 3.55f));
        }

        public void ConfigureStep(int step)
        {JourneyObjectFactory.BeginStage(this,step);
            Stage = Mathf.Min(step, 8); busy = false; clock = weatherClock = 0; SinkDepth = WaterWalkDistance = 0; HandsJoined = false;
            // Detach before replacing the environment; story actor changes keep the same root.
            player.SetParent(game.transform, true); player.localRotation = Quaternion.identity; body.transform.localRotation = Quaternion.identity;
            // SetObjective can leave an old walker pending destruction this frame.
            foreach (var walker in body.GetComponents<DiscipleWalking>()) walker.enabled = false;
            body.maskInteraction = SpriteMaskInteraction.None;
            body.sortingOrder = Stage >= 2 && Stage <= 4 || Stage == 6 || Stage == 7 ? 75 : 50;
            DisposeVisuals(); if (environment) Destroy(environment); environment = JourneyObjectFactory.Create("WaterWalkingEnvironment"); environment.transform.SetParent(transform, false);
            Prayed = Stage >= 2; PeterRescued = Stage >= 7; BothAboard = Stage >= 8; StormStrength = Stage >= 2 ? 1 : 0;
            peter = jesus = waterMask = null; BuildSea();
            if (Stage == 0)
            {
                CurrentPhase = Phase.Crowd; sky.color = C("d7d6af"); sea.color = C("739ba1");
                Shape("기적 뒤의 빈 들", environment.transform, new Vector3(-6, -3), new Vector2(12, 9), "a1ac72", -22);
                for (int i = 0; i < 30; i++) Actor("왕으로 삼으려는 사람 " + i, game.supportingCast[i % 2 == 0 ? 5 : 6], environment.transform, new Vector3(-8.5f + i % 10 * .55f, -3.1f + i / 10 * 1.10f), 1.0f);
                player.position = JourneyStageLayout.Point(this,player,"Placement 4",new Vector3(-1.8f, -.4f)); caption.text = "기적을 보고 예수님을 왕으로 삼으려는 사람들"; caption.color = C("344f43");
            }
            else if (Stage == 1)
            {
                CurrentPhase = Phase.Mountain; boat.gameObject.SetActive(false); foreach (var w in waves) w.gameObject.SetActive(false); foreach (var r in rain) r.gameObject.SetActive(false); ripple.gameObject.SetActive(false);
                sky.color = C("56607b"); sea.color = C("394c60");
                Polygon("먼 산", new[] { new Vector3(-13, -6), new Vector3(-5, 3), new Vector3(4, -6) }, "3e5060", -24);
                Polygon("기도의 산", new[] { new Vector3(-9, -6), new Vector3(0, 1.2f), new Vector3(9, -6) }, "5c655c", -23);
                Shape("산 정상 바위", environment.transform, new Vector3(0, .06f), new Vector2(2.3f, .15f), "939480", 30);
                player.position = JourneyStageLayout.Point(this,player,"Placement 5",new Vector3(0, .14f)); caption.text = "홀로 산에 올라 하나님께 기도하시는 예수님";
            }
            else if (Stage == 2 || Stage == 3)
            {
                CurrentPhase = Stage == 2 ? Phase.Approach : Phase.Reassure; player.position = JourneyStageLayout.Point(this,player,"Placement 6",new Vector3(Stage == 2 ? -7 : -.6f, SurfaceY)); caption.text = Stage == 2 ? "밤 · 예수님께서 바다 위를 걸어오십니다" : "안심하라 · 나니 두려워하지 말라";
            }
            else if (Stage == 4)
            {
                CurrentPhase = Phase.Invitation; disciples[0].gameObject.SetActive(false); player.SetParent(boat, false); player.localPosition = new Vector3(-2.4f, .07f);
                jesus = Actor("물 위의 예수님", game.jesusFrames[0], environment.transform, new Vector3(-2.3f, SurfaceY), 1.85f);
                caption.text = "주님이시라면 · 저를 물 위로 오라 하십시오";
            }
            else if (Stage == 5)
            {
                CurrentPhase = Phase.Sinking; disciples[0].gameObject.SetActive(false); player.position = JourneyStageLayout.Point(this,player,"Placement 7",new Vector3(.6f, SurfaceY)); SetBody(body, sinkingSprite, 1.85f); body.flipX = true;
                jesus = Actor("베드로에게 다가오는 예수님", game.jesusFrames[0], environment.transform, new Vector3(-2.3f, SurfaceY), 1.85f);
                waterMask = MakeWaterMask(body); caption.text = "거센 바람을 보고 두려워진 베드로";
            }
            else if (Stage == 6)
            {
                CurrentPhase = Phase.Rescuing; disciples[0].gameObject.SetActive(false); player.position = JourneyStageLayout.Point(this,player,"Placement 8",new Vector3(-1.3f, SurfaceY)); SetBody(body, rescueSprite, 1.75f); body.flipX = true;
                peter = Actor("구출되는 베드로", sinkingSprite, environment.transform, new Vector3(.6f, SurfaceY - 1.05f), 1.85f);
                peter.GetChild(0).GetComponent<SpriteRenderer>().flipX = true;
                PrepareRescue();
                waterMask = MakeWaterMask(peter.GetChild(0).GetComponent<SpriteRenderer>()); caption.text = "예수님께서 즉시 손을 내밀어 붙잡으십니다";
            }
            else if (Stage == 7)
            {
                CurrentPhase = Phase.Returning; disciples[0].gameObject.SetActive(false); player.position = JourneyStageLayout.Point(this,player,"Placement 9",new Vector3(-.8f, SurfaceY));
                peter = Actor("예수님과 함께 배로 돌아가는 베드로", game.disciples[0], environment.transform, new Vector3(.2f, SurfaceY), 1.85f, true); caption.text = "예수님과 베드로가 함께 배에 오릅니다";
            }
            else
            {
                CurrentPhase = Phase.Calming; disciples[0].gameObject.SetActive(false); player.SetParent(boat, false); player.localPosition = new Vector3(-1.6f, .07f);
                peter = Actor("배에 오른 베드로", game.disciples[0], boat, new Vector3(-.5f, .07f), 1.65f); caption.text = "두 사람이 배에 오르자 · 바람이 그칩니다";
                float[] places = { -2.4f, -2.7f, .2f, 1.2f, 2.2f, 3.2f };
                for (int i = 1; i < disciples.Length; i++) disciples[i].localPosition = new Vector3(places[i], .07f);
            }
            if (jesus) jesus.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 75;
            if (Stage == 0) foreach (Transform child in environment.transform) if (child.name.StartsWith("별 ")) child.gameObject.SetActive(false);
            Weather(0); UpdateCamera(); game.BaptismCinema(IsCinematic); Hud();
        }
        void Weather(float dt)
        {
            if (Stage == 1) return;
            float tilt = StormStrength * (Mathf.Sin(weatherClock * 1.7f) * 8 + Mathf.Sin(weatherClock * 3.2f) * 2);
            boat.rotation = Quaternion.Euler(0, 0, tilt);
            if (CurrentPhase != Phase.Departure) boat.position = JourneyStageLayout.Point(this,boat,"Placement 10",new Vector3(5, .1f + StormStrength * Mathf.Sin(weatherClock * 2.1f) * .14f));
            if (Stage >= 2) { sea.color = Color.Lerp(C("426b7e"), C("244c64"), StormStrength); sky.color = Color.Lerp(C("394d66"), C("253648"), StormStrength); }
            for (int i = 0; i < 12; i++)
            {
                int layer = i / 4; var r = waves[i].GetComponent<SpriteRenderer>(); r.sprite = waveSprites[StormStrength < .25f ? 0 : layer == 0 ? 1 : 2];
                float scale = 6.4f / r.sprite.bounds.size.x; waves[i].localScale = new Vector3(scale, scale * Mathf.Lerp(.34f, .68f, StormStrength), 1);
                waves[i].position = JourneyStageLayout.Point(this,waves[i],"Placement 11",new Vector3(-13 + Mathf.Repeat(weatherClock * (.25f + StormStrength * (.6f + layer * .35f)) + i % 4 * 6.7f + layer, 27), (layer == 0 ? -2.3f : layer == 1 ? -3.65f : -5.25f) + Mathf.Sin(weatherClock * 2 + i) * StormStrength * .15f));
                if (Stage == 0) waves[i].position = JourneyStageLayout.Point(this,waves[i],"Placement 12",new Vector3(4 + Mathf.Repeat(weatherClock * .4f + i % 4 * 4.2f, 17), waves[i].position.y));
                r.color = layer == 0 ? new Color(.66f, .79f, .88f, .8f) : new Color(.78f, .88f, .96f, 1);
            }
            for (int i = 0; i < rain.Length; i++) { rain[i].gameObject.SetActive(StormStrength > .1f); rain[i].position = JourneyStageLayout.Point(this,rain[i],"Placement 13",new Vector3(-10 + i % 14 * 1.5f - Mathf.Repeat(weatherClock * 2, 1.5f), 5 - Mathf.Repeat(weatherClock * 4 + i * .63f, 10))); rain[i].rotation = Quaternion.Euler(0, 0, -25); }
            foreach (var disciple in disciples) disciple.GetChild(0).localRotation = Quaternion.Euler(0, 0, StormStrength * Mathf.Sin(weatherClock * 7 + disciple.GetSiblingIndex()) * 5);
            if (ripple) { ripple.gameObject.SetActive(Stage >= 2 && !BothAboard); ripple.position = player.position + new Vector3(0, .02f); ripple.localScale = Vector3.one * (1 + Mathf.Sin(weatherClock * 4) * .12f); }
        }
        void Hud()
        {
            string[] goals = { "왕으로 삼으려는 군중을 떠나 제자들 먼저 보내기", "산에서 홀로 하나님께 기도하기", "밤바다 위를 걸어 제자들에게 다가가기", "귀신이라 두려워하는 제자들을 안심시키기", "베드로 · 예수님을 향해 물 위 걷기", "두려움과 의심으로 가라앉는 베드로", "예수님께서 베드로를 붙잡아 구하심", "예수님과 베드로 함께 배에 오르기", "잔잔해진 바다에서 제자들의 고백 듣기" };
            string prompt = CurrentPhase == Phase.Approach ? "D / → · 물 위를 걸어 배로 이동    가까이에서 E · 대화" : CurrentPhase == Phase.Walking ? "A / ← · 예수님을 향해 한 걸음씩 걷기" : "E · 이야기 진행";
            game.WaterWalkingHud(goals[Stage], prompt);
        }
        public void Tick(float dt, bool canMove, bool press)
        {
            if (game.Mode == "menu" || game.Mode == "dialogue" || game.Mode == "chapter_end") return;
            clock += dt; weatherClock += dt; Weather(dt);
            switch (CurrentPhase)
            {
                case Phase.Departure:
                    boat.position = JourneyStageLayout.Point(this,boat,"Placement 14",new Vector3(Mathf.Lerp(5, 15, Mathf.Clamp01(clock / 3)), .1f)); caption.text = "예수님께서 제자들을 먼저 배로 보내십니다";
                    if (clock >= 3.3f) game.BaptismAdvance(); return;
                case Phase.Prayer:
                    sky.color = Color.Lerp(C("56607b"), C("253648"), Mathf.Clamp01(clock / 3)); caption.text = "산에서 홀로 기도하시는 동안 · 밤이 깊어갑니다";
                    if (clock >= 3.5f) { Prayed = true; game.BaptismAdvance(); } return;
                case Phase.StepOut:
                    player.position = Vector3.Lerp(new Vector3(2.6f, .17f), new Vector3(2, SurfaceY), Mathf.Clamp01(clock / 1));
                    if (clock >= 1.1f) { CurrentPhase = Phase.Walking; clock = 0; var walking = body.GetComponent<DiscipleWalking>(); if (walking) walking.enabled = true; game.BaptismCinema(false); caption.text = "베드로가 배에서 내려 · 예수님을 향해 걷습니다"; Hud(); } return;
                case Phase.Sinking:
                    SinkDepth = Mathf.Clamp01(clock / 2) * 1.05f; player.position = JourneyStageLayout.Point(this,player,"Placement 15",new Vector3(.6f, SurfaceY - SinkDepth)); ripple.position = JourneyStageLayout.Point(this,ripple,"Placement 16",new Vector3(.6f, SurfaceY)); jesus.position = Vector3.MoveTowards(jesus.position, new Vector3(-1.3f, SurfaceY), dt);
                    if (clock >= 2.4f && !busy) { busy = true; game.MeetingTalk(() => { busy = false; game.BaptismAdvance(); }); } return;
                case Phase.Rescuing:
                    AnimateRescue(clock);
                    if (clock >= 2.6f) { PeterRescued = true; HandsJoined = false; CurrentPhase = Phase.Rescued; waterMask.gameObject.SetActive(false); peter.rotation = Quaternion.identity; peter.position = JourneyStageLayout.Point(this,peter,"Placement 17",new Vector3(Mathf.Max(peter.position.x, player.position.x + 1.5f), SurfaceY)); SetBody(peter.GetChild(0).GetComponent<SpriteRenderer>(), game.disciples[0], 1.85f); SetBody(body, game.jesusFrames[0], 1.85f); game.BaptismCinema(false); Hud(); } return;
                case Phase.Returning:
                    if (clock < 3.1f) { player.position = Vector3.MoveTowards(player.position, new Vector3(2.6f, SurfaceY), dt * 1.25f); peter.position = player.position + new Vector3(.9f, 0); game.MeetingWalk(true, 1); }
                    else if (clock < 4.3f) { float t = Mathf.Clamp01((clock - 3.1f) / 1); player.position = Vector3.Lerp(new Vector3(2.6f, SurfaceY), boat.TransformPoint(new Vector3(-1.6f, .07f)), t); peter.position = Vector3.Lerp(new Vector3(3.5f, SurfaceY), boat.TransformPoint(new Vector3(-.5f, .07f)), t); }
                    else { BothAboard = true; game.BaptismAdvance(); } return;
                case Phase.Calming:
                    StormStrength = 1 - Mathf.Clamp01(clock / 3); Weather(0);
                    if (clock >= 3.2f) { CurrentPhase = Phase.Quiet; StormStrength = 0; caption.text = "바람이 그치고 · 바다가 잠잠해졌습니다"; game.BaptismCinema(false); Hud(); } return;
            }
            if (!canMove || busy) return;
            var k = Keyboard.current; float input = k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            if (CurrentPhase == Phase.Approach)
            {
                player.position = JourneyStageLayout.Point(this,player,"Placement 18",new Vector3(Mathf.Clamp(player.position.x + input * 2.8f * dt, -7, -.6f), SurfaceY)); game.MeetingWalk(input != 0, input);
            }
            else if (CurrentPhase == Phase.Walking)
            {
                var p = player.position; p.x = Mathf.Clamp(p.x + input * 1.9f * dt, .55f, 2.1f); p.y = SurfaceY; player.position = p;
                WaterWalkDistance = Mathf.Max(WaterWalkDistance, 2 - p.x); game.MeetingWalk(input != 0, input);
                if (p.x <= .62f) { busy = true; game.BaptismSay(new[] { "베드로|내가 어떻게 물 위를 걷고 있지? 바람이 이렇게 거센데…!", "내레이션|베드로가 거센 바람을 보고 두려워하며 의심하자, 물속으로 가라앉기 시작했습니다." }, () => { busy = false; game.BaptismAdvance(); }); }
            }
            if (press) Interact();
        }
        public void Interact()
        {
            if (busy || IsCinematic || CurrentPhase == Phase.Walking) return;
            if (CurrentPhase == Phase.Approach && player.position.x < -1.2f) return;
            busy = true; game.MeetingTalk(() =>
            {
                busy = false;
                if (Stage == 0) { CurrentPhase = Phase.Departure; clock = 0; game.BaptismCinema(true); }
                else if (Stage == 1) { CurrentPhase = Phase.Prayer; clock = 0; SetBody(body, prayerSprite, 2.0f); game.BaptismCinema(true); }
                else if (Stage == 4) { player.SetParent(game.transform, true); CurrentPhase = Phase.StepOut; clock = 0; game.BaptismCinema(true); }
                else game.BaptismAdvance();
            });
        }
        public void UpdateCamera() { bool close = Stage == 5 || CurrentPhase == Phase.Rescuing; game.journeyCamera.orthographicSize = close ? 4.3f : 5.625f; game.journeyCamera.transform.position = close ? new Vector3(1.2f, -.1f, -10) : new Vector3(0, .1f, -10); }
        void DisposeVisuals() { foreach (var mesh in meshes) if (mesh) Destroy(mesh); meshes.Clear(); foreach (var mat in ownedMaterials) if (mat) Destroy(mat); ownedMaterials.Clear(); }
        public void Shutdown() { if (shutdown) return; shutdown = true; if (player && game) player.SetParent(game.transform, true); if (pixel) pixel.enabled = oldPixel; DisposeVisuals(); }
        void OnDestroy() { if (shutdown) return; shutdown = true; if (pixel) pixel.enabled = oldPixel; DisposeVisuals(); }
    }
}
