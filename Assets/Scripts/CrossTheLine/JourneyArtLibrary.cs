using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossTheLine
{
    // Authored pixel geometry, rather than a filter over a painted illustration.
    // Unit-square sprites keep every existing interaction and animation coordinate intact.
    public sealed class JourneyArtLibrary : IDisposable
    {
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public Sprite Get(string kind)
        {
            if (sprites.TryGetValue(kind, out var found)) return found;
            var saved=JourneySpriteAssets.Load("CrossTheLine/PixelScenery/"+kind);
            if(saved)return saved;
            int n = kind.StartsWith("Scene") ? 768 : 64;
            bool detailed=kind=="SceneWoodland"||kind=="SceneOlivePrayer"||kind=="SceneOliveRoad"||kind=="SceneGateExterior"||kind=="SceneVillageExterior"||kind=="SceneCityStreet"||kind=="SceneTempleExterior"||kind=="SceneTempleCourt";
            var p = new Canvas(n, detailed?432:n, kind);
            if (kind.StartsWith("Scene")) p.Scene(kind); else p.Prop(kind);
            int width=n,height=n;
            if(kind.StartsWith("Scene"))
            {
                width=960;height=576;
                if(kind=="SceneRiver"){width=1536;height=640;}
                else if(kind=="SceneDesert"||kind=="SceneRooftop"||kind=="SceneMountain"){width=1280;height=640;}
                else if(kind=="SceneField"){width=1088;height=640;}
                else if(kind=="SceneLake"){width=1120;height=640;}
                else if(kind=="SceneSea"){width=1280;height=480;}
                else if(kind=="SceneSky"){width=1280;height=160;}
                if(detailed){width=768;height=432;}
            }
            int logicalHeight=detailed?432:n;
            var output=new Color32[width*height];for(int y=0;y<height;y++)for(int x=0;x<width;x++)output[y*width+x]=p.pixels[(y*logicalHeight/height)*n+x*n/width];
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "NativePixel_" + kind, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(output); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, width, 0, SpriteMeshType.FullRect);
            sprite.name = "NativePixel_" + kind; sprites.Add(kind, sprite); return sprite;
        }
        public void Dispose() { foreach (var s in sprites.Values) { if(Application.isPlaying){if(s.texture)UnityEngine.Object.Destroy(s.texture);UnityEngine.Object.Destroy(s);}else{if(s.texture)UnityEngine.Object.DestroyImmediate(s.texture);UnityEngine.Object.DestroyImmediate(s);} } sprites.Clear(); }

        sealed class Canvas
        {
            public readonly Color32[] pixels;
            readonly int w, h; readonly System.Random rng;
            public Canvas(int width, int height, string seed) { w=width; h=height; pixels=new Color32[w*h]; int hash=17; foreach(char c in seed)hash=unchecked(hash*31+c); rng=new System.Random(hash & int.MaxValue); }
            Color32 C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c);return c; }
            void B(int x,int y,int width,int height,string hex) { var c=C(hex);for(int yy=Math.Max(0,y);yy<Math.Min(h,y+height);yy++)for(int xx=Math.Max(0,x);xx<Math.Min(w,x+width);xx++)pixels[yy*w+xx]=c; }
            void E(int x,int y,int rx,int ry,string hex) { for(int yy=-ry;yy<=ry;yy++){int dx=(int)(rx*Math.Sqrt(Math.Max(0,1-yy*yy/(double)(ry*ry))));B(x-dx,y+yy,2*dx+1,1,hex);} }
            void L(int x,int y,int x2,int y2,int thick,string hex) { int d=Math.Max(Math.Abs(x2-x),Math.Abs(y2-y));for(int i=0;i<=d;i++){float t=d==0?0:i/(float)d;B((int)Mathf.Lerp(x,x2,t),(int)Mathf.Lerp(y,y2,t),thick,thick,hex);} }
            void Brick(int x,int y,int width,int height,int bw,int bh) { B(x,y,width,height,"B99D78");for(int yy=0;yy<height;yy+=bh)for(int xx=-(yy/bh%2)*bw/2;xx<width;xx+=bw){int left=Math.Max(0,xx),right=Math.Min(width,xx+bw-2);if(right>left){B(x+left,y+yy,right-left,Math.Min(bh-2,height-yy),rng.Next(3)==0?"CEB28C":"D8C09A");B(x+left,y+yy+bh-3,right-left,1,"E5CFA9");}} }
            void Floor(bool indoor) { B(0,0,w,h,indoor?"C8B493":"B7AE83");for(int y=0;y<h;y+=18){int size=28+y/22;for(int x=-(y/18%2)*size/2;x<w;x+=size){B(x+1,y+1,size-2,16,indoor?(rng.Next(4)==0?"C3AE8D":"D7C3A0"):(rng.Next(4)==0?"BEAA82":"D4BE96"));B(x+3,y+15,size-6,1,"E5CEAB");}}for(int i=0;i<650;i++)B(rng.Next(w),rng.Next(h),2,1,indoor?"B9A481":"A59D73"); }
            void Cloud(int x,int y) { E(x,y,35,7,"E6E3C3");E(x-12,y+5,20,10,"F0ECD3");E(x+13,y+3,23,8,"F0ECD3"); }
            void Tree(int x,int y) { E(x,y-3,32,6,"A39973");B(x-4,y,8,62,"684D36");B(x-3,y+3,3,53,"947047");L(x,y+32,x-20,y+59,5,"684D36");L(x,y+35,x+21,y+67,4,"684D36");E(x,y+75,39,28,"435D43");E(x-24,y+66,25,23,"56734A");E(x+23,y+76,25,22,"6C8450");for(int i=0;i<70;i++){int dx=rng.Next(-30,31),dy=rng.Next(-18,19);if(dx*dx+dy*dy<850)B(x+dx,y+75+dy,3,2,i%3==0?"A0A36A":"7E955C");} }
            void House(int x,int y,int width=92) { Brick(x,y,width,85,23,12);B(x-4,y+84,width+8,7,"715C46");B(x-6,y+91,width+12,4,"E1C59B");B(x+width/2-12,y,24,43,"54483A");E(x+width/2,y+42,12,12,"54483A");B(x+width/2-9,y+3,18,38,"80613F");for(int i=0;i<4;i++)B(x+width/2-8+i*4,y+4,1,35,"A17B4C");B(x+13,y+45,17,21,"65533D");B(x+width-30,y+45,17,21,"65533D");B(x+10,y+43,23,3,"E6C99D");B(x+width-33,y+43,23,3,"E6C99D");B(x-7,y-6,width+14,6,"A68F6B"); }
            void Jar(int x,int y,int size=24) { E(x,y,size,size*3/2,"625244");E(x-2,y+2,size-3,size*3/2-3,"A38B68");E(x-7,y+5,size/3,size,"C4AB81");B(x-size/2,y+size,size,8,"A9916F");E(x,y+size+8,size/2+2,3,"D5BC93");E(x,y+size+9,size/2-2,2,"514439"); }
            public void Scene(string kind)
            {
                if(kind=="SceneWoodland"||kind=="SceneOlivePrayer"||kind=="SceneOliveRoad"||kind=="SceneGateExterior"||kind=="SceneVillageExterior"||kind=="SceneCityStreet"||kind=="SceneTempleExterior"||kind=="SceneTempleCourt"){DetailedScene(kind);return;}
                if(kind=="SceneDesert"||kind=="SceneRooftop"||kind=="SceneMountain") { Wilderness(kind);return; }
                if(kind=="SceneRiver"||kind=="SceneField") { Nature(kind);return; }
                if(kind=="SceneSea"||kind=="SceneSky")
                {
                    B(0,0,w,h,kind=="SceneSea"?"D0D0D0":"F0F0F0");
                    if(kind=="SceneSea")for(int i=0;i<1700;i++){int x=rng.Next(w),y=rng.Next(h);B(x,y,rng.Next(2,12),1,i%5==0?"EFEFEF":"C6C6C6");}
                    else for(int i=0;i<10;i++){int x=20+i*85,y=170+i%3*50;E(x,y,70,12,"DEDEDE");E(x+17,y+8,46,15,"E6E6E6");}
                    return;
                }
                bool room=kind=="SceneWedding"||kind=="SceneHome"||kind=="SceneSynagogue"||kind=="SceneTemple";
                Floor(room);
                if(room)
                {
                    Brick(0,480,w,288,48,24);B(0,477,w,9,"A98B66");B(0,486,w,3,"EAD5AE");
                    for(int x=80;x<w;x+=152){B(x-9,480,18,215,"BDA780");B(x-7,480,9,213,"EFDFB9");B(x+4,480,6,213,"D5C09B");B(x-16,480,32,8,"BCA17B");B(x-14,684,28,12,"E6D3AE");}
                    B(w/2-27,480,54,99,"625140");E(w/2,578,27,25,"625140");B(w/2-23,481,46,85,"8E6D46");for(int i=-19;i<22;i+=7)B(w/2+i,486,2,83,"B39565");
                    if(kind=="SceneWedding"){for(int x=28;x<w-28;x+=6){int y=679-(int)(18*Math.Sin((x-28)/(float)(w-56)*Math.PI));B(x,y,6,6,"984643");}for(int x=48;x<w;x+=48){L(x,679,x,651,1,"937853");E(x,649,4,5,"CEAD61");}for(int i=0;i<4;i++)Jar(40+i*22,500+i%2*23,9);}
                    if(kind=="SceneSynagogue"){B(335,0,97,450,"B59772");for(int y=8;y<450;y+=30){B(347,y,73,2,"C9AC80");B(350,y+4,67,1,"CEB38C");}B(301,460,166,12,"A38763");B(306,473,156,5,"E3CAA2");}
                    if(kind=="SceneTemple"){for(int i=0;i<3;i++){B(0,450-i*12,w,10,i%2==0?"D7C19D":"BCA480");B(0,458-i*12,w,2,"F0DEBB");}}
                    for(int x=105;x<w;x+=280){B(x,525,3,38,"645139");E(x+2,564,5,9,"C47D3F");E(x+2,569,3,6,"F0CD79");B(x-5,525,15,4,"826544");}
                }
                else
                {
                    B(0,500,w,268,"BACFBC");B(0,624,w,144,"A5C6C6");for(int i=0;i<8;i++)Cloud(45+i*105,663+i%3*21);
                    for(int x=0;x<w;x++){int top=508+(int)(30*Math.Sin(x*.017)+22*Math.Sin(x*.007));B(x,490,1,Math.Max(0,top-490),"8CA397");}
                    if(kind=="SceneLake"){B(0,0,w,490,"578C98");for(int i=0;i<500;i++)B(rng.Next(w),rng.Next(0,485),rng.Next(3,17),1,i%4==0?"B3CEBF":"74A6A8");}
                    else {for(int i=0;i<6;i++)House(35+i*130,480+i%2*18,85);Tree(118,433);Tree(645,425);Jar(693,412,11);B(0,193,w,126,"D6BF95");for(int i=0;i<600;i++)B(rng.Next(w),rng.Next(194,317),2,1,"BBA780");}
                }
            }
            void Olive(int x,int y,int size)
            {
                E(x,y-2,size/2,6,"7E805C");
                L(x,y,x+7,y+size*2/3,13,"574C38");L(x-3,y,x+2,y+size*2/3,5,"9D8357");
                L(x+3,y+size/3,x-size/3,y+size*3/4,9,"574C38");L(x+6,y+size/2,x+size/3,y+size*4/5,8,"574C38");
                E(x,y+size*4/5,size/2,size/3,"344E3C");E(x-size/3,y+size*3/4,size/3,size/4,"496546");E(x+size/3,y+size*4/5,size/3,size/4,"617B4B");
                for(int i=0;i<size*3;i++){int dx=rng.Next(-size/2,size/2),dy=rng.Next(-size/3,size/3);if(dx*dx/(float)(size*size/4)+dy*dy/(float)(size*size/9)<.88f){int px=x+dx,py=y+size*4/5+dy;B(px,py,3,2,i%5==0?"A2AA68":i%3==0?"81955C":"56784D");if(i%7==0)B(px+1,py+2,2,1,"BCC07C");}}
            }
            void Arch(int x,int y,int width,int height)
            {
                B(x,y,width,height,"514B3D");E(x+width/2,y+height,width/2,width/2,"514B3D");
                B(x+5,y,width-10,height,"856947");for(int i=7;i<width-6;i+=7)B(x+i,y,1,height,"A88A59");
                B(x+3,y+height/3,width-6,3,"594C38");B(x+3,y+height*3/4,width-6,3,"594C38");
                for(int i=0;i<=12;i++){float a=i*Mathf.PI/12;int px=x+width/2+(int)((width/2+5)*Mathf.Cos(a)),py=y+height+(int)((width/2+5)*Mathf.Sin(a));B(px-3,py-3,6,6,"E5D3AA");B(px-2,py+2,5,1,"F3E2B9");}
            }
            void DetailedScene(string kind)
            {
                bool temple=kind.Contains("Temple"),town=kind=="SceneCityStreet"||kind=="SceneVillageExterior",gate=kind=="SceneGateExterior";
                bool nature=!temple&&!town&&!gate;
                B(0,0,w,h,nature?"A1AA77":"D0B991");B(0,300,w,132,"9CBABC");B(0,373,w,59,"A9C6C8");
                for(int i=0;i<8;i++){int x=25+i*105,y=354+i%3*19;E(x,y,31,4,"D9E0CB");E(x+11,y+4,20,6,"EBE9D2");}
                for(int x=0;x<w;x++){int y=286+(int)(18*Math.Sin(x*.018)+12*Math.Sin(x*.007));B(x,270,1,y-270,"728F82");int yy=268+(int)(15*Math.Sin(x*.011+1));B(x,239,1,yy-239,"889A75");}
                if(nature)
                {
                    bool prayer=kind=="SceneOlivePrayer";if(prayer){B(0,373,w,59,"DACAA1");E(651,381,15,15,"F0DCAC");}
                    for(int i=0;i<18;i++){int x=260+i*22,y=260+i%4*3;Brick(x,y,15+i%3*5,13+i%3*4,8,6);B(x-1,y+13+i%3*4,17+i%3*5,2,"D9C29A");}
                    if(prayer){B(411,274,7,24,"DCC69A");E(414,298,8,8,"C6AC72");B(460,265,5,32,"7E886F");}
                    for(int x=0;x<w;x++){int edge=198+(int)(17*Math.Sin(x*.011));B(x,0,1,edge,"A3AD7A");}
                    for(int y=0;y<230;y++){int center=kind=="SceneWoodland"?376:390+y/5;int half=45+(230-y)/2;B(center-half,y,half*2,1,"D2BC8D");B(center-half,y,3,1,"B5A579");B(center+half,y,3,1,"E3CDA0");}
                    for(int i=0;i<4200;i++){int x=rng.Next(w),y=rng.Next(0,238);int center=kind=="SceneWoodland"?376:390+y/5,half=45+(230-y)/2;if(x<center-half-3||x>center+half+3){B(x,y,1,2,i%3==0?"748653":"BAC28A");if(i%31==0)B(x+1,y+2,2,1,"E6DDB0");}else if(i%4==0)B(x,y,2,1,i%8==0?"B8A47E":"E1CCA0");}
                    for(int i=0;i<12;i++)Olive(18+i*68,227+i%2*8,49+i%3*6);
                    Olive(33,109,180);Olive(733,123,165);if(kind=="SceneWoodland"){Olive(125,200,102);Olive(661,203,103);}
                    for(int i=0;i<20;i++){int x=rng.Next(w),y=rng.Next(10,120);if(x<180||x>585){E(x,y,6+i%4,3,"78816A");E(x-1,y+1,5+i%3,2,"B6B796");}}
                    return;
                }
                for(int y=0;y<300;y+=14){int bw=28+(300-y)/12;for(int x=-(y/14%2)*bw/2;x<w;x+=bw){B(x+1,y+1,bw-2,12,"D8C29A");B(x+2,y+11,bw-4,1,"EAD4AB");B(x+4,y+2,bw-9,1,"C1AB83");if(rng.Next(8)==0)L(x+7,y+3,x+11,y+7,1,"AE9977");}}
                if(gate)
                {
                    Brick(403,226,365,183,25,13);B(400,405,368,6,"EDD7AB");
                    for(int i=0;i<12;i++){B(412+i*31,410,20,18,"D5BC91");B(414+i*31,426,18,2,"EFDBB1");}
                    Brick(405,217,67,209,23,13);Brick(676,217,73,209,23,13);B(399,416,81,7,"E6CEA4");B(670,416,87,7,"E6CEA4");
                    Arch(520,223,109,111);B(520,223,109,102,"BAC4A0");for(int i=0;i<4;i++)House(531+i*23,224,20);B(520,223,109,18,"DEC79B");
                    Olive(49,171,149);Olive(214,229,60);for(int i=0;i<25;i++){int x=20+i*11,y=209+i%3*2;B(x,y,3,4,"71865C");}return;
                }
                if(temple)
                {
                    int level=kind=="SceneTempleCourt"?296:260;
                    Brick(0,level,w,h-level,29,15);B(0,level-8,w,8,"AF9875");B(0,level,w,4,"E9D6B1");
                    for(int i=0;i<10;i++){int x=20+i*81;Arch(x+11,level+15,46,391-level);B(x,level-3,11,405-level,"BCA783");B(x+2,level-3,4,405-level,"F1E1BD");B(x+8,level-3,3,405-level,"95876E");B(x-5,level-7,22,7,"D8C5A0");B(x-5,399,22,7,"ECDCBA");for(int yy=level+20;yy<391;yy+=18)B(x+5,yy,1,9,"D2C2A0");}
                    B(0,406,w,7,"DFCCA7");B(0,413,w,4,"9A8669");B(0,419,w,13,"E6D5B2");for(int x=6;x<w;x+=12)B(x,421,3,6,"B99C71");
                    for(int i=0;i<3;i++){B(0,level-19-i*10,w,8,"D3BF9B");B(0,level-12-i*10,w,1,"F0DDB9");}
                    if(kind=="SceneTempleExterior"){Brick(235,251,298,175,29,15);Arch(343,252,80,91);B(228,423,312,9,"F0DCB6");}
                    return;
                }
                if(kind=="SceneCityStreet")
                {
                    Brick(0,250,w,182,27,14);for(int i=0;i<6;i++){int x=11+i*135;Arch(x+37,251,45,57);B(x-3,323,129,7,"715E43");B(x,330,122,17,"CDB691");B(x+3,344,115,2,"EFDBB2");B(x+10,359,23,28,"554D3C");B(x+15,362,3,22,"AD9870");B(x+90,359,23,28,"554D3C");}
                    for(int i=0;i<6;i++){int x=12+i*135;B(x,246,113,6,"AE8F69");for(int j=0;j<7;j++)B(x+j*16,306,16,14,j%2==0?"A15E4C":"D7B988");B(x,302,113,4,"654E36");Jar(x+19,250,7);Jar(x+98,250,8);}return;
                }
                for(int i=0;i<7;i++){int x=20+i*119;House(x,255+i%2*9,89);Jar(x+70,250,8);}
                Olive(100,216,60);Olive(654,214,61);
            }
            void Nature(string kind)
            {
                B(0,0,w,h,"A5AE77");B(0,510,w,h-510,"C9D3BB");B(0,626,w,h-626,"ACC9C4");for(int i=0;i<8;i++)Cloud(30+i*105,670+i%3*18);
                for(int x=0;x<w;x++)
                {
                    int top=527+(int)(Math.Sin(x*.013)*24+Math.Sin(x*.028)*12);B(x,500,1,Math.Max(0,top-500),"839C7D");B(x,486,1,25,"A7B58B");
                    if(kind=="SceneRiver")
                    {
                        float wx=(x-w/2f)/w*48,center=-wx*.04f+Mathf.Sin(wx*.3f)*.2f;int low=(int)((center-1.72f+10)/20*h),high=(int)((center+1.05f+10)/20*h);
                        B(x,0,1,low-5,"CFBD95");B(x,low-5,1,5,"A3A87A");B(x,low,1,high-low,"588F91");B(x,high,1,6,"B8BC89");
                        if(x%17==0){B(x,high+4,2,10,"658056");B(x+3,high+4,1,7,"84945E");}
                    }
                    else if(x>460)B(x,495,1,20,"96BAB1");
                }
                for(int i=0;i<5400;i++)
                {
                    int x=rng.Next(w),y=rng.Next(0,493);
                    float wx=(x-w/2f)/w*48,center=-wx*.04f+Mathf.Sin(wx*.3f)*.2f;float wy=(y-h/2f)/h*20;
                    if(kind=="SceneRiver"&&wy>center-1.72f&&wy<center+1.05f)B(x,y,rng.Next(3,11),1,i%4==0?"BED0BB":"79ADAA");
                    else {B(x,y,1,2,i%3==0?"7D935D":"B7BD87");if(i%29==0)B(x+1,y+2,2,1,"E5D8A9");}
                }
                for(int i=0;i<13;i++){int x=25+i*62,y=491+rng.Next(16);Tree(x,y);}
                for(int i=0;i<26;i++){int x=rng.Next(w),y=rng.Next(80,240);E(x,y,3,1,"958D70");}
            }
            void Wilderness(string kind)
            {
                B(0,0,w,h,"DCC5A4");B(0,530,w,h-530,"B3C9C1");for(int i=0;i<7;i++)Cloud(60+i*110,633+i%2*28);E(101,650,17,17,"F1DCAA");
                for(int x=0;x<w;x++)
                {
                    int distant=404+(int)(Math.Sin(x*.014)*35+Math.Sin(x*.006)*27);B(x,0,1,distant,kind=="SceneDesert"?"B69E80":"899B94");
                    int near=346+(int)(Math.Sin(x*.009+1)*28);B(x,0,1,near,kind=="SceneDesert"?"CFB28A":"697D78");
                    if(kind=="SceneDesert"){int dune=283+(int)(Math.Sin(x*.008)*18);B(x,0,1,dune,"DCC399");B(x,dune-2,1,2,"EED5A9");}
                }
                if(kind=="SceneDesert")
                {
                    for(int i=0;i<3200;i++)B(rng.Next(w),rng.Next(0,313),2,1,i%3==0?"AF977A":"E8CF9F");
                    for(int i=0;i<12;i++){int x=30+i*63,y=230+rng.Next(22);E(x,y,7+rng.Next(8),4,"8A7E6C");E(x-2,y+2,6,2,"B6A185");}
                    for(int y=30;y<190;y+=22)L(0,y,w,y+30,1,"C7AE85");
                }
                else
                {
                    for(int i=0;i<23;i++)House(i*35,210+rng.Next(45),28);
                    if(kind=="SceneRooftop")
                    {
                        Brick(206,0,355,419,37,18);B(197,419,373,16,"E8D3AD");B(196,411,375,5,"A18B70");
                        for(int x=230;x<550;x+=52){B(x,120,10,271,"F0DCB8");B(x+10,120,5,271,"AE997D");B(x-5,390,25,10,"D1B895");}
                        B(356,111,52,124,"5F584C");E(382,234,26,23,"5F584C");
                    }
                    else
                    {
                        for(int x=0;x<w;x++){int top=(x>254&&x<448)?435:435-(int)(Math.Abs(x-350)*.76);if(top>0){B(x,0,1,top,"767D6D");B(x,top-5,1,5,"C4B491");}}
                        for(int i=0;i<45;i++){int x=rng.Next(210,470),y=rng.Next(40,380);L(x,y,x+25,y+8,2,"666F64");}
                        Tree(262,435);
                    }
                }
            }
            public void Prop(string kind)
            {
                switch(kind)
                {
                    case "Stone": Brick(0,0,64,64,16,10);break;
                    case "Floor": Floor(true);break;
                    case "Path": Floor(false);break;
                    case "Wood": B(0,0,64,64,"8E6844");for(int y=0;y<64;y+=13){B(0,y,64,2,"604A35");B(0,y+3,64,1,"BE965E");for(int x=8;x<64;x+=19)B(x,y+5,11,1,"9F7A4E");}E(44,33,5,2,"705439");break;
                    case "Table": B(7,0,5,42,"594632");B(53,0,5,42,"594632");B(9,0,2,36,"9B774C");B(55,0,2,36,"9B774C");B(0,39,64,23,"745437");B(0,44,64,18,"AC824F");B(1,59,62,3,"D2AD76");B(0,43,64,2,"533F2E");for(int y=48;y<58;y+=4)B(1,y,62,1,"947049");break;
                    case "Leaves": E(32,32,31,28,"405B40");for(int i=0;i<40;i++){int x=rng.Next(5,58),y=rng.Next(8,57);if((x-32)*(x-32)+(y-32)*(y-32)<650)E(x,y,5,4,i%2==0?"6D854F":"8A985B");}break;
                    case "Trunk": B(12,0,40,64,"614935");B(17,0,27,64,"8B6744");B(18,0,5,64,"AA8350");for(int i=0;i<12;i++)L(rng.Next(24,47),rng.Next(64),rng.Next(24,47),rng.Next(64),1,"725539");break;
                    case "Door": B(0,0,64,64,"564535");B(5,2,54,58,"806343");for(int x=7;x<58;x+=8)B(x,2,2,58,"A58355");B(5,12,54,3,"594C3D");B(5,45,54,3,"594C3D");E(49,29,3,3,"D3B66A");break;
                    case "Window": B(0,0,64,64,"BFA582");B(7,5,50,55,"52483C");B(11,7,42,49,"7A6951");for(int x=16;x<56;x+=14)B(x,6,3,50,"C1AA82");B(7,32,50,3,"D6BF93");break;
                    case "Basket": E(32,9,24,8,"76523A");B(7,9,50,43,"B28950");for(int y=9;y<48;y+=5){B(8,y,48,1,"775635");for(int x=9;x<56;x+=8)B(x,y,2,4,"D5AF70");}E(32,50,26,9,"E0BC7E");E(32,51,22,6,"674C34");break;
                    case "Plate": E(32,30,31,25,"866644");E(32,32,30,24,"D8C59B");E(32,34,25,19,"F0DFB9");E(27,35,17,12,"B48B52");E(25,38,16,10,"DBB573");for(int i=0;i<3;i++)L(18+i*7,35,20+i*7,42,1,"A07843");break;
                    case "Coin": E(32,32,29,28,"957036");E(30,35,27,25,"DCB96A");E(30,35,21,20,"BB914E");E(29,36,18,18,"D8B361");B(27,21,5,27,"AB813F");B(23,33,15,3,"EFD287");break;
                    case "Cup": B(9,12,46,47,"D1B27C");E(32,58,23,5,"F0D7A3");E(32,59,19,3,"68513E");B(14,16,5,35,"EDD19B");B(49,17,4,33,"A88856");E(32,12,23,7,"AA8355");break;
                    case "Jar": E(32,27,28,24,"635348");E(30,29,26,23,"9F8B70");E(22,31,14,21,"C3B08E");B(15,47,34,11,"A89578");B(17,51,4,9,"D8C5A3");E(32,58,20,5,"E2CEAC");E(32,59,15,3,"544A3D");E(30,9,20,4,"A79578");for(int y=19;y<42;y+=6)B(37,y,17,1,"8B7963");break;
                    case "Scroll": B(5,7,54,50,"EAD8AF");B(4,5,7,54,"AA8358");B(53,5,7,54,"AA8358");B(7,7,3,50,"F3E5C3");B(56,7,2,50,"D8BC89");for(int y=17;y<48;y+=5)B(16,y,29-y%7,1,"9C8866");break;
                    case "Sail": for(int y=0;y<64;y++){int extent=16+(int)(46*Math.Sin((y+4)/72f*Math.PI));B(3,y,extent,1,"E0CFAB");B(4,y,Math.Max(1,extent-7),1,"F1E3C1");}L(4,0,4,63,2,"B19A75");L(4,5,44,34,1,"CFBB95");break;
                    case "Hull": for(int y=0;y<64;y++){int inset=(int)(15*Math.Pow(1-y/64f,2));B(inset,y,64-inset*2,1,y%11<2?"5D4433":"98704A");if(y%11==3)B(inset+2,y,60-inset*2,1,"C39760");}B(0,60,64,4,"D4AE76");break;
                    case "Fish": E(33,33,23,14,"547D80");E(32,37,22,11,"B4C9AF");E(34,40,19,5,"D7D8B4");for(int x=1;x<16;x++)B(x,26-x/2,1,14+x,"85A4A0");E(49,38,2,2,"303F3E");L(23,46,29,53,2,"779894");break;
                    case "Water": B(0,0,64,64,"609699");for(int i=0;i<24;i++)B(rng.Next(58),rng.Next(64),rng.Next(3,12),1,i%3==0?"B4CDC0":"79ADAA");break;
                }
            }
        }
    }
}
