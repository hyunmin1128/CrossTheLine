using System;
using UnityEngine;

namespace CrossTheLine
{
    // A deterministic pixel landscape. Geometry is rendered into a point-filtered texture.
    public sealed class JourneyWorld : IDisposable
    {
        public GameObject Root { get; private set; }
        Texture2D texture;
        Sprite sprite;
        readonly Color32[] pixels = new Color32[1536 * 360];
        const int W = 1536, H = 360;
        System.Random random;

        Color32 C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        void Box(int x, int y, int w, int h, Color32 color)
        {
            for (int yy = Mathf.Max(0, y); yy < Mathf.Min(H, y + h); yy++)
                for (int xx = Mathf.Max(0, x); xx < Mathf.Min(W, x + w); xx++) pixels[yy * W + xx] = color;
        }
        void Hill(int cx, int baseY, int radius, int height, Color32 color)
        {
            for (int x = cx - radius; x < cx + radius; x += 4)
            {
                int top = Mathf.RoundToInt(height * Mathf.Clamp01(1 - Mathf.Abs(x - cx) / (float)radius));
                Box(x, baseY, 4, top / 4 * 4, color);
            }
        }
        void Palm(int x, int y, int height, bool night)
        {
            Box(x, y, 5, height, C(night ? "343b45" : "826a49"));
            for (int i = 0; i < 6; i++)
            {
                int dx = (i - 3) * 9;
                Box(x + dx, y + height - Mathf.Abs(i - 3) * 4, 19, 5, C(night ? "293e43" : "536e50"));
                Box(x + dx + 4, y + height - Mathf.Abs(i - 3) * 4 - 4, 13, 4, C(night ? "293e43" : "536e50"));
            }
        }
        void House(int x, int y, int width, int height, bool night)
        {
            Box(x + 3, y - 3, width, height, C("534f48"));
            Box(x, y, width, height, C(night ? "515768" : "c7b18b"));
            Box(x - 4, y + height - 4, width + 8, 7, C(night ? "696b79" : "e5caa0"));
            Box(x + width / 2 - 6, y, 12, 24, C("4c4946"));
            Box(x + 9, y + height - 22, 9, 12, C(night ? "d4a56c" : "706b60"));
            Box(x + width - 18, y + height - 22, 9, 12, C(night ? "d4a56c" : "706b60"));
            for (int i = 6; i < width - 6; i += 13) Box(x + i, y + height - 3, 1, 4, C("ae9678"));
            for(int yy=5;yy<height-4;yy+=8)for(int xx=(yy/8%2)*7;xx<width;xx+=15)Box(x+xx,y+yy,12,1,C(night?"626776":"b6a080"));
            for(int yy=0;yy<23;yy+=6)Box(x+width/2-5,y+yy,10,1,C("785a40"));
        }
        public void Build(string biome, int seed, Material spriteMaterial)
        {
            if(seed>=17)
            {
                string kind=biome=="room"?"SceneHome":biome=="temple"?"SceneTemple":biome=="garden"?"SceneOliveRoad":biome=="river"?"SceneRiver":biome=="lake"||biome=="storm"?"SceneSea":biome=="desert"||biome=="cross"?"SceneDesert":"SceneVillage";
                // Legacy lake chapters walk along the shore, not on the open sea.
                if(biome=="lake")kind="SceneRiver";
                var background=JourneySpriteAssets.Load(biome=="garden"?"CrossTheLine/OlivesPrayerLandscape":"CrossTheLine/PixelScenery/"+kind);
                if(background){Dispose();Root=new GameObject("Landscape_"+biome);Root.transform.position=new Vector3(24,0,4);var image=Root.AddComponent<SpriteRenderer>();image.sprite=background;image.sharedMaterial=spriteMaterial;image.sortingOrder=-100;Root.transform.localScale=new Vector3(48/background.bounds.size.x,11.25f/background.bounds.size.y,1);return;}
            }
            if(seed==0)
            {
                var game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();var authored=game?game.authoredPreviewRoot:null;
                if(authored){Dispose();Root=UnityEngine.Object.Instantiate(authored);Root.SetActive(true);Root.name="Landscape_"+biome;Root.GetComponent<SpriteRenderer>().sharedMaterial=spriteMaterial;return;}
            }
            Dispose(); random = new System.Random(seed + 731);
            bool night = biome == "night" || biome == "storm" || biome == "garden";
            bool water = biome == "river" || biome == "lake" || biome == "storm";
            bool city = biome == "city" || biome == "temple" || biome == "room";
            bool desert = biome == "desert" || biome == "cross";
            Color32 sky = C(night ? "182d46" : desert ? "e2baa0" : "b7d2c8");
            Box(0, 0, W, H, sky);
            Box(0, 180, W, 34, C(night ? "23394e" : "c7d6bb"));
            Box(0, 142, W, 38, C(night ? "2b4559" : "d5d9b9"));
            if (night) for (int i = 0; i < 110; i++) Box(random.Next(W), random.Next(235, 354), 1, 1, C("cad6cf"));
            Box(220, 276, 22, 22, C(night ? "e4d5ad" : "f4e2b3"));
            Box(216, 280, 30, 14, C(night ? "e4d5ad" : "f4e2b3"));
            for (int i = 0; i < 13; i++) Hill(i * 144, 132, random.Next(100, 220), random.Next(38, 100), C(night ? "314959" : "8eaa9a"));
            for (int i = 0; i < 14; i++) Hill(i * 132, 113, random.Next(90, 180), random.Next(20, 60), C(night ? "3e575b" : "7e987d"));
            if (water)
            {
                Box(0, 91, W, 62, C(night ? "31516b" : "699d9d"));
                for (int i = 0; i < 230; i++) Box(random.Next(W), random.Next(94, 151), random.Next(4, 20), 1, C(night ? "628296" : "adc9b2"));
                for (int i = 0; i < 5; i++)
                {
                    int x = 100 + i * 300; Box(x, 111, 49, 8, C("7c6048")); Box(x + 5, 107, 39, 4, C("634f43"));
                    Box(x + 21, 119, 2, 36, C("634f43")); Hill(x + 24, 120, 16, 32, C("e2d5b3"));
                }
            }
            if (city)
            {
                for (int i = 0; i < 18; i++) House(i * 91 - 12, 113, random.Next(48, 78), random.Next(36, 82), night);
                if (biome == "temple")
                {
                    Box(560, 125, 270, 84, C("d7c6aa")); Box(544, 203, 300, 9, C("e9d9ba"));
                    for (int x = 568; x < 816; x += 40) { Box(x, 125, 12, 77, C("f1dfbd")); Box(x + 12, 125, 5, 77, C("bba888")); }
                    for (int i = 0; i < 4; i++) Box(548 - i * 4, 118 - i * 5, 292 + i * 8, 5, C("c7b38e"));
                }
            }
            Color32 ground = C(night ? "5a655e" : desert ? "c5a27a" : "a9aa79");
            Box(0, 0, W, 98, ground); Box(0, 58, W, 27, C(night ? "8c8973" : "d3c09a"));
            Box(0, 85, W, 3, C(night ? "686e60" : "b8a981"));
            for (int i = 0; i < 1100; i++) Box(random.Next(W), random.Next(0, 97), 2, 1, C(night ? "6b7464" : "b9b98a"));
            for(int yy=60;yy<84;yy+=6)for(int xx=(yy/6%2)*10;xx<W;xx+=20){Box(xx,yy,18,1,C(night?"706f60":"b59f7d"));Box(xx,yy+1,1,4,C(night?"777767":"c2ad87"));}
            if(seed==0)
            {
                // A quiet Bethlehem stable beside the remembered Nativity, on the existing route.
                House(340,98,138,77,true);Box(359,98,98,55,C("343c43"));
                for(int i=0;i<5;i++)Box(354+i*24,156,25,6+i%2*2,C("8c765b"));
                Box(385,104,48,8,C("ab8e61"));Box(390,99,4,9,C("735b42"));Box(424,99,4,9,C("735b42"));
                for(int i=0;i<18;i++)Box(382+random.Next(50),108+random.Next(4),3,1,C("d6bb83"));
                Box(405,283,3,27,C("f1dfb0"));Box(397,292,19,3,C("f1dfb0"));Box(408,213,1,62,C("697b86"));
            }
            for (int i = 0; i < 22; i++)
            {
                int x = random.Next(W); Palm(x, random.Next(88, 101), random.Next(32, 56), night);
                if (!desert && !night) { Box(x + 30, 90, 2, 4, C("557e58")); Box(x + 29, 94, 4, 2, C("ead1ac")); }
            }
            if (biome == "cross")
                for (int i = 0; i < 3; i++) { int x = 630 + i * 80; Box(x, 99, 5, 72 - i * 5, C("615044")); Box(x - 19, 146 - i * 5, 43, 5, C("615044")); }
            if (biome == "tomb") { Hill(735, 98, 94, 85, C("8a927c")); Box(710, 98, 35, 43, C("404b46")); Box(760, 98, 28, 34, C("a6aa8d")); }
            texture = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "JourneyLandscape" };
            texture.SetPixels32(pixels); texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32);
            Root = new GameObject("Landscape_" + biome);
            Root.transform.position = new Vector3(24, 0, 4);
            var sr = Root.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = -100;
            sr.sharedMaterial = spriteMaterial;
        }
        public void Dispose()
        {
            if (Root) UnityEngine.Object.Destroy(Root);
            if (sprite) UnityEngine.Object.Destroy(sprite);
            if (texture) UnityEngine.Object.Destroy(texture);
            Root = null;
        }
    }
}
