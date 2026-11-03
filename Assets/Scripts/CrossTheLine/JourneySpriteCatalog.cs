using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CrossTheLine
{
    [CreateAssetMenu(menuName="CrossTheLine/Pixel Sprite Catalog")]
    public sealed class JourneySpriteCatalog : ScriptableObject
    {
        [Serializable] public class Item { public Sprite original;public string resource;public Sprite pixel;public bool character;public Rect visibleBounds; }
        public List<Item> items=new List<Item>();
    }
    public static class JourneySpriteAssets
    {
        // The editable stage bank persists the runtime unit square as a 32px PNG.
        // Its import size changes; it is still a placeholder, not finished scenery.
        public static bool IsPrimitive(Sprite sprite)=>sprite&&(sprite.texture.width==1||sprite.name=="_0_0_1_1"||sprite.texture.name=="_0_0_1_1");
        static JourneySpriteCatalog catalog;
        static Dictionary<Sprite,Sprite> remap;
        static Dictionary<Sprite,Bounds> geometry;
        public static bool Ready=>JourneyAssets.Load<JourneySpriteCatalog>("CrossTheLine/JourneySpriteCatalog")!=null;
        static void Prepare()
        {
            if(remap!=null)return;catalog=JourneyAssets.Load<JourneySpriteCatalog>("CrossTheLine/JourneySpriteCatalog");
            remap=new Dictionary<Sprite,Sprite>();geometry=new Dictionary<Sprite,Bounds>();if(catalog)foreach(var i in catalog.items){if(i.original&&i.pixel)remap[i.original]=i.pixel;if(i.pixel&&i.visibleBounds.width>0){var r=i.visibleBounds;geometry[i.pixel]=new Bounds(new Vector3(r.center.x,r.center.y,0),new Vector3(r.width,r.height,0));}}
        }
        public static Sprite Resolve(Sprite source){Prepare();return source&&remap.TryGetValue(source,out var pixel)?pixel:source;}
        public static Sprite[] LoadAll(string resource)
        {
            Prepare();if(catalog){var result=catalog.items.Where(i=>i.resource==resource&&i.pixel).Select(i=>i.pixel).ToArray();if(result.Length>0)return result;}
            return JourneyAssets.LoadAll<Sprite>(resource).Select(Resolve).ToArray();
        }
        public static Sprite Load(string resource)=>LoadAll(resource).FirstOrDefault();
        public static Bounds VisibleBounds(Sprite sprite)
        {
            Prepare();return geometry.TryGetValue(sprite,out var bounds)?bounds:sprite.bounds;
        }
        public static void Reset(){catalog=null;remap=null;geometry=null;}
    }
}
