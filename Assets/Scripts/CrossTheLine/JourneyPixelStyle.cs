using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossTheLine
{
    // Display all chapter art on the same dot grid as the user's native 64px Jesus.
    // Sprite rectangles, body bounds, hand anchors and gameplay data stay authoritative.
    [DefaultExecutionOrder(10000)]
    public sealed class JourneyPixelStyle : MonoBehaviour
    {
        [System.Serializable] class HeadItem {public string name;public float cx,cy,h;}
        [System.Serializable] class HeadData {public HeadItem[] items;}
        Dictionary<string,HeadItem> headAnchors;
        JourneyGame game;
        Material dotMaterial;Sprite nativeHead,dot;Texture2D dotTexture;Color skin;
        readonly Dictionary<SpriteRenderer,SpriteRenderer> heads=new Dictionary<SpriteRenderer,SpriteRenderer>();
        readonly Dictionary<SpriteRenderer,Material> originals=new Dictionary<SpriteRenderer,Material>();
        MaterialPropertyBlock block;
        readonly HashSet<Texture> nativeTextures=new HashSet<Texture>();
        SpriteRenderer[] renderers=new SpriteRenderer[0];
        
        public int StyledCount {get;private set;}
        public float PixelsPerUnit {get;private set;}
        public void Initialize(JourneyGame owner)
        {
            game=owner;headAnchors=JsonUtility.FromJson<HeadData>(JourneyAssets.Load<TextAsset>("CrossTheLine/JourneyHeadAnchors").text).items.ToDictionary(h=>h.name);block=new MaterialPropertyBlock();foreach(var s in game.jesusFrames.Concat(game.disciples).Concat(game.supportingCast))if(s)nativeTextures.Add(s.texture);dotMaterial=new Material(JourneyAssets.Load<Shader>("CrossTheLine/JourneyDot"));
            dotMaterial.SetTexture("_DotPalette",JourneyAssets.Load<Texture2D>("CrossTheLine/JourneyDotPalette"));
            dotMaterial.SetTexture("_JesusPalette",JourneyAssets.Load<Texture2D>("CrossTheLine/JourneyJesusPalette"));
            var headTexture=JourneyAssets.Load<Texture2D>("CrossTheLine/OriginalJesusPixels");dotMaterial.SetTexture("_SourceJesus",headTexture);skin=headTexture.GetPixel(36,50);dotTexture=new Texture2D(1,1);dotTexture.SetPixel(0,0,Color.white);dotTexture.Apply();dot=Sprite.Create(dotTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);nativeHead=Sprite.Create(headTexture,new Rect(22,38,24,20),new Vector2(.5f,.5f),64,0,SpriteMeshType.FullRect);
        }
        bool Generated(Sprite s)=>s&&s.texture&&!s.texture.name.StartsWith("NativePixel_")&&!nativeTextures.Contains(s.texture)&&s.texture.width>512&&s.texture.height>128;
        public void RefreshNow(){renderers=FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);}
        void LateUpdate()
        {
            if(!game||!dotMaterial)return;if(block==null)block=new MaterialPropertyBlock();
            RefreshNow();
            var native=renderers.FirstOrDefault(r=>r&&r.sprite&&game.jesusFrames.Contains(r.sprite)&&r.enabled&&r.gameObject.activeInHierarchy);
            PixelsPerUnit=native?native.sprite.rect.height/Mathf.Max(.1f,native.bounds.size.y):game.SecondTemple?64/2.8f:64/1.85f;
            StyledCount=0;
            foreach(var r in renderers)
            {
                if(!r||!r.sprite)continue;
                if(!Generated(r.sprite))
                {
                    if(heads.TryGetValue(r,out var oldHead)&&oldHead)oldHead.enabled=false;
                    if(originals.TryGetValue(r,out var original)&&r.sharedMaterial==dotMaterial){r.sharedMaterial=original;block.Clear();r.SetPropertyBlock(block);}
                    continue;
                }
                if(!originals.ContainsKey(r))originals[r]=r.sharedMaterial;
                r.sharedMaterial=dotMaterial;
                var s=r.sprite;var rect=s.rect;var size=Vector2.Scale(s.bounds.size,new Vector2(Mathf.Abs(r.transform.lossyScale.x),Mathf.Abs(r.transform.lossyScale.y)));
                var grid=new Vector2(Mathf.Max(2,Mathf.Round(size.x*PixelsPerUnit)),Mathf.Max(2,Mathf.Round(size.y*PixelsPerUnit)));
                block.Clear();
                block.SetVector("_SpriteRect",new Vector4(rect.x/s.texture.width,rect.y/s.texture.height,rect.width/s.texture.width,rect.height/s.texture.height));
                block.SetVector("_PixelGrid",new Vector4(grid.x,grid.y,0,0));
                HeadPlacement(s,out var head,out var left);ApplyNativeHead(r,head,left);
                block.SetFloat("_HeadKind",s.name=="JesusThanksgiving"?1:s.name=="SleepingJesus"?2:0);
                block.SetFloat("_IsJesus",s.name.Contains("Jesus")||s.texture.name=="EntryGriefAtlas"?1:0);
                block.SetFloat("_WorldArt",s.rect.width>900&&s.rect.height>600&&s.texture.name.Contains("Landscape")||s.texture.name.Contains("Exterior")||s.texture.name.Contains("Courtyard")?1:0);block.SetFloat("_HeadTears",s.texture.name=="EntryGriefAtlas"&&s.name!="Grief0"?1:0);
                block.SetVector("_HeadRect",head);block.SetFloat("_HeadLeft",left?1:0);
                r.SetPropertyBlock(block);StyledCount++;
            }
            foreach(var dead in originals.Keys.Where(r=>!r).ToArray()){originals.Remove(dead);heads.Remove(dead);}
        }
        void ApplyNativeHead(SpriteRenderer body,Vector4 rect,bool left)
        {
            if(rect.z<=0){if(heads.TryGetValue(body,out var old)&&old)old.enabled=false;return;}
            if(!heads.TryGetValue(body,out var head)||!head)
            {
                head=new GameObject("OriginalJesusDotHead").AddComponent<SpriteRenderer>();head.transform.SetParent(body.transform,false);head.sprite=nativeHead;head.sharedMaterial=game.spriteMaterial;heads[body]=head;
                for(int i=0;i<5;i++){var pixel=new GameObject(i==0?"NativeDotTear":"ClosedEye"+i).AddComponent<SpriteRenderer>();pixel.transform.SetParent(head.transform,false);pixel.sprite=dot;pixel.sharedMaterial=game.spriteMaterial;pixel.gameObject.SetActive(false);}
            }
            head.enabled=body.enabled;head.color=body.color;head.sortingLayerID=body.sortingLayerID;head.sortingOrder=body.sortingOrder+1;head.maskInteraction=body.maskInteraction;head.gameObject.layer=body.gameObject.layer;
            var bounds=body.sprite.bounds;float x=bounds.min.x+(rect.x+rect.z*.5f)*bounds.size.x,y=bounds.min.y+(rect.y+rect.w*.5f)*bounds.size.y;
            head.transform.localPosition=new Vector3(body.flipX?-x:x,y,0);
            head.transform.localScale=new Vector3(bounds.size.x*rect.z/nativeHead.bounds.size.x,bounds.size.y*rect.w/nativeHead.bounds.size.y,1);head.flipX=left^body.flipX;head.transform.localRotation=Quaternion.Euler(0,0,0);
            for(int i=0;i<5;i++)
            {
                var pixel=head.transform.GetChild(i).GetComponent<SpriteRenderer>();pixel.sortingOrder=head.sortingOrder+1;pixel.sortingLayerID=head.sortingLayerID;pixel.maskInteraction=head.maskInteraction;pixel.gameObject.layer=body.gameObject.layer;
                bool grief=body.sprite.texture.name=="EntryGriefAtlas"&&body.sprite.name!="Grief0",sleep=body.sprite.name=="SleepingJesus";
                pixel.gameObject.SetActive(head.enabled&&(i==0?grief:sleep));
                float xPixel=i==0?6.5f:i==1||i==3?0:4;
                pixel.transform.localPosition=new Vector3((head.flipX?-xPixel:xPixel)/64,i==0?-4.5f/64:-2f/64,0);
                pixel.transform.localScale=new Vector3((i==0?1:3)/64f,(i==0?2:i<=2?2:1)/64f,1);
                pixel.color=(i==0?new Color(.41f,.61f,.65f):i<=2?skin:new Color(.18f,.12f,.1f))*body.color;
                pixel.sortingOrder=head.sortingOrder+(i>=3?2:1);
            }
        }
        void HeadPlacement(Sprite s,out Vector4 rect,out bool left)
        {
            rect=Vector4.zero;left=false;
            string n=s.name,t=s.texture.name;
            bool jesus=n.Contains("Jesus")||t=="EntryGriefAtlas";
            if(!jesus)return;
            float cx=.40f,cy=.83f,h=.34f;
            if(n=="Jesus0Rebuke"){cx=.28f;cy=.83f;}
            else if(n.Contains("Push")){cx=.33f;cy=.81f;}
            else if(n=="Jesus3Welcome"){cx=.42f;cy=.83f;}
            else if(n=="JesusBlessing"){cx=.68f;cy=.80f;left=true;}
            else if(n=="JesusShare"){cx=.60f;cy=.84f;left=true;}
            else if(n=="JesusReceive"){cx=.43f;cy=.84f;left=true;}
            else if(n=="JesusThanksgiving"){cx=.50f;cy=.84f;}
            else if(n=="JesusRescuing"){cx=.35f;cy=.83f;}
            else if(n.StartsWith("Mounted")){cx=.44f;cy=.85f;h=.23f;}
            else if(t=="EntryGriefAtlas"){cx=.42f;cy=.82f;h=.23f;}
            else if(n=="JesusPointing"){cx=.30f;cy=.85f;}
            else if(n=="SleepingJesus"){cx=.22f;cy=.58f;h=.65f;}
            if(headAnchors.TryGetValue(n,out var anchor)){cx=anchor.cx;cy=anchor.cy;h=anchor.h;}
            float w=Mathf.Min(.75f,h*1.2f*s.rect.height/s.rect.width);cx-=w*.10f;
            cy=Mathf.Clamp(cy,h*.5f,1-h*.5f);rect=new Vector4(cx-w*.5f,cy-h*.5f,w,h);
        }
        void OnDestroy()
        {
            foreach(var pair in originals)if(pair.Key&&pair.Key.sharedMaterial==dotMaterial){pair.Key.sharedMaterial=pair.Value;block.Clear();pair.Key.SetPropertyBlock(block);}
            foreach(var head in heads.Values)if(head)Destroy(head.gameObject);if(nativeHead)Destroy(nativeHead);if(dot)Destroy(dot);if(dotTexture)Destroy(dotTexture);if(dotMaterial)Destroy(dotMaterial);
        }
    }
}
