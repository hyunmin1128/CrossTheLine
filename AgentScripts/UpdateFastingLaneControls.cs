using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TMPro;
using CrossTheLine.Minigames;
public static class UpdateFastingLaneControls
{
    const string Art="Assets/Sprite/Minigames/FastingTopDown/",Pref="Assets/Prefabs/Minigames/FastingTopDown/";
    static Color32[] palette={new Color32(0,0,0,0),new Color32(25,21,20,255),new Color32(58,34,24,255),new Color32(88,48,30,255),new Color32(118,67,38,255),new Color32(151,91,52,255),new Color32(80,14,24,255),new Color32(133,23,36,255),new Color32(185,35,47,255),new Color32(246,239,219,255),new Color32(218,207,186,255),new Color32(177,161,137,255),new Color32(239,184,123,255),new Color32(196,131,81,255),new Color32(110,70,42,255),new Color32(255,249,235,255)};
    static Color32 Quant(Color32 c){if(c.a<128)return palette[0];int best=1;float min=float.MaxValue;for(int i=1;i<palette.Length;i++){float d=(c.r-palette[i].r)*(c.r-palette[i].r)+(c.g-palette[i].g)*(c.g-palette[i].g)+(c.b-palette[i].b)*(c.b-palette[i].b);if(d<min){min=d;best=i;}}return palette[best];}
    static void Attach(GameObject art,RuntimeAnimatorController controller){var a=art.GetComponent<Animator>();if(!a)a=art.AddComponent<Animator>();a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!g||g.gameObject.scene.isDirty)throw new Exception("Open saved top-down scene");
        var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes(Art+"GeneratedJesusWalk_Source.png"));int cell=raw.width/4;
        var bounds=new RectInt[4];int maxW=0,maxH=0;
        for(int f=0;f<4;f++){int minX=cell,minY=raw.height,maxX=0,maxY=0;for(int y=0;y<raw.height;y++)for(int x=0;x<cell;x++)if(raw.GetPixel(f*cell+x,y).a>.5f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}bounds[f]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);maxW=Math.Max(maxW,bounds[f].width);maxH=Math.Max(maxH,bounds[f].height);}
        float scale=Mathf.Min(54f/maxW,58f/maxH);var sprites=new Sprite[4];
        for(int f=0;f<4;f++){var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);var b=bounds[f];float center=b.x+b.width*.5f;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float sx=center+(x-31.5f)/scale,sy=b.y+(y-3)/scale;tex.SetPixel(x,y,sx>=0&&sx<cell&&sy>=b.y&&sy<b.yMax?Quant(raw.GetPixel(f*cell+Mathf.FloorToInt(sx),Mathf.FloorToInt(sy))):Color.clear);}tex.Apply();string path=Art+"TempJesusWalk_"+f+".png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.SaveAndReimport();sprites[f]=AssetDatabase.LoadAssetAtPath<Sprite>(path);}
        UnityEngine.Object.DestroyImmediate(raw);
        string clipPath=Pref+"TempJesusWalk.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}clip.frameRate=8;var keys=new ObjectReferenceKeyframe[5];for(int n=0;n<5;n++)keys[n]=new ObjectReferenceKeyframe{time=n/8f,value=sprites[n%4]};AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);var cs=AnimationUtility.GetAnimationClipSettings(clip);cs.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,cs);
        string controllerPath=Pref+"TempJesusWalk.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller){controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var state=controller.layers[0].stateMachine.AddState("WalkForever");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;}
        var hero=PrefabUtility.LoadPrefabContents(Pref+"TempJesusTopDown.prefab");Attach(hero.transform.Find("Artwork").gameObject,controller);hero.transform.Find("Artwork").GetComponent<SpriteRenderer>().sprite=sprites[0];PrefabUtility.SaveAsPrefabAsset(hero,Pref+"TempJesusTopDown.prefab");PrefabUtility.UnloadPrefabContents(hero);Attach(g.playerBody.gameObject,controller);g.playerBody.sprite=sprites[0];g.idle=sprites[0];
        var guide=g.introduction.transform.Find("FiveItemGuide").GetComponent<TMP_Text>();guide.text="자동으로 앞으로 걷습니다. 좌우 키를 누르면 한 칸 즉시 이동합니다.\n위아래 이동 불가 · 키를 길게 눌러도 한 번만 이동\nSpace 유지: 멈춰 기도 / 놓기: 자동 전진 재개\n\n1. 수통 [E] : 정화 + 지침 1칸 회복 + 잠시 가속\n2. 믿음의 방패 : 다음 충돌 자동 방어 후 소모\n3. 성령의 불꽃 : 기도 속도 증가 / 3개마다 잠시 피버\n4. 성경 완본 100% / 페이지 25% / 조각 10% 기도량\n5. 물 보급 : 수통 충전 + 보호 (마지막 구간 없음)\n\n기도 목표를 채운 뒤 위쪽 종착지로 가세요.\n지침 5칸 · 피해 1칸 · 실패 시 1일부터 재시작";
        var controls=g.help.transform.parent.Find("Controls").GetComponent<TMP_Text>();controls.text="A/D 또는 ←/→ : 한 칸 이동 · 자동 전진 · Space 기도 · E 수통 · R 재시작";
        foreach(var t in new[]{guide,controls}){PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.playerBody);EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();return "Saved four 64x64 16-color walk frames, looping Animator, lane controls guide, scripture amounts.";
    }
}
