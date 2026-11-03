using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossTheLine
{
    [DefaultExecutionOrder(9500)]
    public sealed class JourneyPresentation : MonoBehaviour
    {
        JourneyGame game; JourneyArtLibrary art;
        SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        readonly Dictionary<SpriteRenderer, SpriteRenderer> shadows = new Dictionary<SpriteRenderer, SpriteRenderer>();
        readonly HashSet<SpriteRenderer> treated = new HashSet<SpriteRenderer>();
        readonly HashSet<Texture> actors = new HashSet<Texture>();
        Texture2D shadowTexture; Sprite shadowSprite;
        Material wardrobe; MaterialPropertyBlock wardrobeBlock;
        readonly Dictionary<SpriteRenderer,Color> clothing=new Dictionary<SpriteRenderer,Color>();
        readonly Dictionary<SpriteRenderer,Vector3> crowdPrevious=new Dictionary<SpriteRenderer,Vector3>();
        readonly Dictionary<SpriteRenderer,float> crowdPhase=new Dictionary<SpriteRenderer,float>();
        float scanAt; int chapter=-1;
        public int DetailedProps {get;private set;}
        public int GroundedActors => shadows.Count;
        public void Initialize(JourneyGame owner)
        {
            game=owner;art=new JourneyArtLibrary();
            wardrobe=new Material(JourneyAssets.Load<Shader>("CrossTheLine/JourneyWardrobe"));wardrobeBlock=new MaterialPropertyBlock();
            foreach(var s in game.jesusFrames.Concat(game.disciples).Concat(game.supportingCast))if(s)actors.Add(s.texture);
            var poses=game.GetComponent<JourneyChapterSpriteSet>();if(poses)foreach(var s in new[]{poses.jesusIdle,poses.jesusBaptism,poses.johnIdle,poses.johnWaiting,poses.johnReady,poses.johnBaptizing})if(s)actors.Add(s.texture);
            shadowTexture=new Texture2D(48,16,TextureFormat.RGBA32,false){name="PixelContactShadow",filterMode=FilterMode.Point};
            var p=new Color32[48*16];for(int y=0;y<16;y++)for(int x=0;x<48;x++){float v=(x-23.5f)*(x-23.5f)/576+(y-7.5f)*(y-7.5f)/64;p[y*48+x]=new Color32(36,43,39,(byte)(v<.45f?49:v<.75f?35:v<1?18:0));}
            shadowTexture.SetPixels32(p);shadowTexture.Apply();shadowSprite=Sprite.Create(shadowTexture,new Rect(0,0,48,16),Vector2.one*.5f,48,0,SpriteMeshType.FullRect);
        }
        void LateUpdate()
        {
            if(!game)return;
            if(chapter!=game.ChapterIndex){chapter=game.ChapterIndex;scanAt=0;}
            if(Time.unscaledTime>=scanAt)
            {
                renderers=game.GetComponent<JourneyContinuousWorld>()?game.GetComponentsInChildren<SpriteRenderer>(true):FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);scanAt=Time.unscaledTime+.12f;
                foreach(var r in renderers)
                {
                    if(!r||!r.sprite||r.name.StartsWith("OriginalJesus")||r.name.StartsWith("NativeDot")||r.name.StartsWith("ClosedEye"))continue;
                    if(!treated.Contains(r)&&JourneySpriteAssets.IsPrimitive(r.sprite)){TreatPrimitive(r);treated.Add(r);}
                    if(r.name=="SketchLandscape"&&!treated.Contains(r)&&game.Temptation&&!game.GetComponent<TemptationScrollSettings>())
                    {
                        var size=r.sprite.bounds.size;r.sprite=art.Get(game.Temptation.TrialIndex==0?"SceneDesert":game.Temptation.TrialIndex==1?"SceneRooftop":"SceneMountain");r.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1);treated.Add(r);
                    }
                    if(r.name=="SketchBasedJordanRiver"&&!treated.Contains(r))
                    {
                        var size=r.sprite.bounds.size;r.sprite=art.Get("SceneRiver");r.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1);treated.Add(r);
                    }
                    DressCrowd(r);
                    if(IsActor(r)&&!shadows.ContainsKey(r))AddShadow(r);
                }
                foreach(var dead in shadows.Keys.Where(r=>!r).ToArray()){if(shadows[dead])Destroy(shadows[dead].gameObject);shadows.Remove(dead);}
                treated.RemoveWhere(r=>!r);
                foreach(var dead in clothing.Keys.Where(r=>!r).ToArray()){clothing.Remove(dead);crowdPrevious.Remove(dead);crowdPhase.Remove(dead);}
            }
            foreach(var pair in shadows)
            {
                var body=pair.Key;var shade=pair.Value;if(!body||!shade)continue;
                bool wet=body.maskInteraction!=SpriteMaskInteraction.None||body.name.Contains("Water")||body.sprite.name.Contains("Rescuing")||body.sprite.name.Contains("Sinking");
                shade.enabled=body.enabled&&body.gameObject.activeInHierarchy&&!wet;
                if(!shade.enabled)continue;
                var b=body.bounds;var visible=JourneySpriteAssets.VisibleBounds(body.sprite);var contact=body.transform.TransformPoint(new Vector3(visible.center.x,visible.min.y,0));shade.transform.position=new Vector3(contact.x,contact.y+.025f,body.transform.position.z+.02f);
                shade.transform.rotation=Quaternion.identity;
                float width=Mathf.Clamp(b.size.x*.62f,.08f,1.15f);shade.transform.localScale=new Vector3(width,.24f*Mathf.Clamp(b.size.y/1.85f,.16f,1.4f),1);
                shade.sortingLayerID=body.sortingLayerID;shade.sortingOrder=body.sortingOrder-1;shade.color=new Color(1,1,1,body.color.a);
            }
            foreach(var pair in clothing)
            {
                var r=pair.Key;if(!r||!r.sprite)continue;var s=r.sprite;var rect=s.rect;
                float distance=Vector3.Distance(r.transform.position,crowdPrevious[r]);crowdPrevious[r]=r.transform.position;
                bool walking=Time.deltaTime>0&&distance>Time.deltaTime*.05f&&distance<2&&game.Mode!="menu"&&game.Mode!="dialogue";
                crowdPhase[r]=walking?crowdPhase[r]+Time.deltaTime*12:0;
                float stride=0;
                r.sharedMaterial=wardrobe;wardrobeBlock.Clear();wardrobeBlock.SetColor("_ClothTint",pair.Value);wardrobeBlock.SetVector("_WalkTexel",new Vector4(1f/s.texture.width,1f/s.texture.height,stride,0));wardrobeBlock.SetVector("_SpriteRect",new Vector4(rect.x/s.texture.width,rect.y/s.texture.height,rect.width/s.texture.width,rect.height/s.texture.height));r.SetPropertyBlock(wardrobeBlock);
            }
        }
        void DressCrowd(SpriteRenderer r)
        {
            if(clothing.ContainsKey(r)||!game.supportingCast.Contains(r.sprite))return;
            string n=r.name+" "+(r.transform.parent?r.transform.parent.name:"");
            if(!(n.Contains("손님")||n.Contains("상인")||n.Contains("사람")||n.Contains("세리")||n.Contains("부모")||n.Contains("주인")||n.Contains("종")||n.Contains("바리새인")))return;
            if(r.sprite==game.supportingCast[0]||r.sprite==game.supportingCast[1]||r.sprite==game.supportingCast[3]||r.sprite==game.supportingCast[4])return;
            string[] palette={"728574","8B655B","AD9667","687D8A","A18582","8B8062","627268","AD8562"};
            int hash=7;foreach(char c in n)hash=unchecked(hash*31+c);ColorUtility.TryParseHtmlString("#"+palette[(hash&int.MaxValue)%palette.Length],out var color);clothing.Add(r,color);crowdPrevious[r]=r.transform.position;crowdPhase[r]=0;
        }
        bool IsActor(SpriteRenderer r)
        {
            if(actors.Contains(r.sprite.texture))return true;
            string n=r.sprite.name;
            return r.bounds.size.y>.65f&&(n.Contains("Disciple")||n.Contains("Jesus")||n.StartsWith("Merchant")||n.StartsWith("Resident")||n.StartsWith("ChildPose")||n.StartsWith("Leader")||n.StartsWith("Peter")||n.StartsWith("Andrew")||n.StartsWith("Grief"));
        }
        void AddShadow(SpriteRenderer body)
        {
            // Existing chapter-specific contact shadows remain authoritative.
            var root=body.transform.parent;
            if(root&&root.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r!=body&&(r.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0||r.name.Contains("그림자"))))return;
            var go=new GameObject("PolishedGroundContact");go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=shadowSprite;r.sharedMaterial=game.spriteMaterial;shadows.Add(body,r);
        }
        void TreatPrimitive(SpriteRenderer r)
        {
            string n=r.name,kind=null;
            // Background replacement is limited to the original block-based chapters.
            if(n=="땅"&&game.Meeting)kind=game.Meeting.SceneStep==1?"SceneHome":"SceneVillage";
            else if(n=="시몬의 집 바닥")kind="SceneHome";
            else if(n=="잔칫집 바닥")kind=game.Wedding?"SceneWedding":"SceneHome";
            else if(n=="성전 돌바닥")kind="SceneTemple";
            else if(n=="회당 바닥")kind="SceneSynagogue";
            else if(n=="호수"&&game.Fishing)kind="SceneLake";
            else if(n=="가버나움 길")kind="SceneVillage";
            else if(n=="빈 들의 풀밭")kind="SceneField";
            else if(n=="바다")kind="SceneSea";
            else if(n=="하늘")kind="SceneSky";
            else if(n.Contains("돌바닥")||n.Contains("기둥")||n.Contains("벽")||n=="마을 담"||n=="석조 집"||n=="마태의 집")kind="Stone";
            else if(n.Contains("바닥"))kind="Floor";
            else if(n.Contains("길")||n=="얕은 물과 물가")kind="Path";
            else if(n.Contains("식탁")&&!n.Contains("보")||n.Contains("잔칫상")||n.Contains("장사하는 상")||n.Contains("책상")||n.Contains("읽는 자리"))kind="Table";
            else if(n.Contains("나뭇잎"))kind="Leaves";
            else if(n.Contains("나무 줄기")||n.Contains("돛대"))kind="Trunk";
            else if(n=="문"||n.Contains("출입문")||n=="열린 문")kind="Door";
            else if(n=="창")kind="Window";
            else if(n=="바구니")kind="Basket";
            else if(n=="몸통"&&r.transform.parent&&r.transform.parent.name.StartsWith("돌항아리"))
            {
                kind="Jar";r.transform.localScale=new Vector3(1.1f,1.55f,1);r.transform.localPosition=new Vector3(0,.18f,0);
                foreach(Transform child in r.transform.parent)if(child.name=="항아리 어깨"||child.name=="목"||child.name=="입")child.gameObject.SetActive(false);
            }
            else if(n.Contains("접시"))kind="Plate";
            else if(n.Contains("동전"))kind="Coin";
            else if(n=="잔"||n=="포도주가 되는 잔")kind="Cup";
            else if(n=="두루마리"||n.Contains("장부"))kind="Scroll";
            else if(n=="돛")kind="Sail";
            else if(n=="나무 선체")kind="Hull";
            else if(n.StartsWith("물고기 "))kind="Fish";
            else if(n=="지붕"||n=="집 지붕"||n=="갑판"||n.Contains("판자")||n=="상 위"||n.Contains("긴 의자"))kind="Wood";
            if(kind==null)return;
            r.sprite=art.Get(kind);var scale=r.transform.localScale;r.transform.localScale=new Vector3(scale.x/r.sprite.bounds.size.x,scale.y/r.sprite.bounds.size.y,scale.z);if(kind!="SceneSea"&&kind!="SceneSky")r.color=new Color(1,1,1,r.color.a);DetailedProps++;
            if(kind.StartsWith("Scene")&&kind!="SceneField"&&kind!="SceneSea"&&kind!="SceneSky")r.sortingOrder=-10;
            if(kind=="SceneField")foreach(Transform child in r.transform.parent)if(child.name=="저녁 하늘"||child.name=="먼 언덕"||child.name=="갈릴리 호수의 먼 물빛")child.gameObject.SetActive(false);
        }
        void OnDestroy()
        {
            foreach(var s in shadows.Values)if(s)Destroy(s.gameObject);
            art?.Dispose();if(shadowSprite)Destroy(shadowSprite);if(shadowTexture)Destroy(shadowTexture);if(wardrobe)Destroy(wardrobe);
        }
    }
}
