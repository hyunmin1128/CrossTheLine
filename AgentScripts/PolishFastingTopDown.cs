using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CrossTheLine.Minigames;
public static class PolishFastingTopDown
{
    const string Art="Assets/Sprite/Minigames/FastingTopDown/",Pref="Assets/Prefabs/Minigames/FastingTopDown/";
    static Sprite Icon(string name,Func<int,int,int> pixel,Color32 main)
    {
        var t=new Texture2D(24,24,TextureFormat.RGBA32,false);for(int y=0;y<24;y++)for(int x=0;x<24;x++){int value=pixel(x,y);t.SetPixel(x,y,value==0?Color.clear:value==1?new Color32(25,23,25,255):value==3?new Color32(255,232,169,255):main);}t.Apply();string path=Art+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g)throw new Exception("Open top-down scene");
        Sprite down=Icon("TempDownArrow",(x,y)=>{bool fill=(y>=7&&y<21&&Math.Abs(x-12)<=2)||(y>=2&&y<=12&&Math.Abs(x-12)<=y-2);return fill?2:0;},new Color32(255,248,210,255));
        var hz=PrefabUtility.LoadPrefabContents(Pref+"TelegraphedHazard.prefab");var arrow=hz.GetComponent<FastingTopDownHazard>().arrow;arrow.sprite=down;arrow.transform.localScale=Vector3.one;PrefabUtility.SaveAsPrefabAsset(hz,Pref+"TelegraphedHazard.prefab");PrefabUtility.UnloadPrefabContents(hz);
        Sprite water=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/Well.png").OfType<Sprite>().SingleOrDefault();
        if(!water)throw new Exception("Import the user's Well.png sprite before rebuilding water points.");
        Sprite shield=Icon("TempShield",(x,y)=>{int w=y<10?y/2+2:8;if(y<2||y>21||Math.Abs(x-12)>w)return 0;return Math.Abs(x-12)>=w-1||y>=20?1:Math.Abs(x-12)<1?3:2;},new Color32(61,190,199,255));
        Sprite flame=Icon("TempFlame",(x,y)=>{int w=y<8?y+1:Math.Max(1,(23-y)/2);if(y<2||y>22||Math.Abs(x-12)>w)return 0;return Math.Abs(x-12)==w?1:y<10&&Math.Abs(x-12)<3?3:2;},new Color32(246,132,35,255));
        Sprite book=Icon("TempBook",(x,y)=>x<3||x>21||y<4||y>20?0:x<5||x>19||y<6||y>18||x==12?1:y%4==0?3:2,new Color32(198,163,102,255));
        Sprite page=Icon("TempPage",(x,y)=>x<5||x>18||y<3||y>21?0:x==5||x==18||y==3||y==21?1:y%4==0&&x>8&&x<16?1:2,new Color32(231,211,162,255));
        Sprite fragment=Icon("TempFragment",(x,y)=>x<7||x>17||y<7||y>17||x+y>31?0:x==7||y==7||x==17||y==17?1:2,new Color32(216,190,139,255));
        Sprite rock=Icon("TempRock",(x,y)=>{bool inside=y>=4&&y<=19&&x>=Math.Max(2,8-y/2)&&x<=Math.Min(21,15+y/2)&&!(y>16&&(x<6||x>18));if(!inside)return 0;return x<5||x>19||y==4||y==19?1:y>13?3:2;},new Color32(138,112,82,255));
        foreach(var e in g.entities){if(e.kind==FastingTopDownEntity.Kind.Cover){var r=e.transform.Find("Rock").GetComponent<SpriteRenderer>();r.sprite=rock;r.transform.localScale=new Vector3(2.2f,1.45f,1);r.color=Color.white;e.transform.Find("RockOutline").gameObject.SetActive(false);e.marker.sortingOrder=8;e.marker.color=new Color(.1f,.8f,.85f,.4f);continue;}var s=e.transform.Find("Symbol").GetComponent<SpriteRenderer>();s.sprite=e.kind==FastingTopDownEntity.Kind.Water?water:e.kind==FastingTopDownEntity.Kind.Shield?shield:e.kind==FastingTopDownEntity.Kind.Flame?flame:e.kind==FastingTopDownEntity.Kind.Book?book:e.kind==FastingTopDownEntity.Kind.Page?page:fragment;s.transform.localScale=Vector3.one*1.4f;s.color=Color.white;e.transform.Find("SymbolOutline").gameObject.SetActive(false);}
        g.playerBody.transform.localPosition=new Vector3(0,.85f,0);g.player.Find("Shadow").localPosition=new Vector3(0,-.02f,0);g.feet.sortingOrder=35;g.feet.color=new Color(1,1,1,.65f);
        var hero=PrefabUtility.LoadPrefabContents(Pref+"TempJesusTopDown.prefab");hero.transform.Find("Artwork").localPosition=new Vector3(0,.85f,0);hero.transform.Find("Shadow").localPosition=new Vector3(0,-.02f,0);hero.transform.Find("Footprint_ActualHitArea").GetComponent<SpriteRenderer>().sortingOrder=35;PrefabUtility.SaveAsPrefabAsset(hero,Pref+"TempJesusTopDown.prefab");PrefabUtility.UnloadPrefabContents(hero);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Distinct icons, visible shelter zones, and aligned player foot marker saved.";
    }
}
