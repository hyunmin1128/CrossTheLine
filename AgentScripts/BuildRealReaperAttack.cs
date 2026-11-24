using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildRealReaperAttack
{
    public static string Run(){
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        const string folder="Assets/Animations/Minigames/FastingTopDown/RealReaper";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprite/Objects/Chapter3/Stage4/RealReaperAttack.aseprite").OfType<AnimationClip>().Single(c=>c.name=="Attack");
        var binding=AnimationUtility.GetObjectReferenceCurveBindings(source).Single(b=>b.propertyName=="m_Sprite");var keys=AnimationUtility.GetObjectReferenceCurve(source,binding);
        string clipPath=AssetDatabase.GenerateUniqueAssetPath(folder+"/RealReaperAttack.anim");var clip=new AnimationClip{name="RealReaperAttack",frameRate=100};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="Artwork",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        var clipSettings=AnimationUtility.GetAnimationClipSettings(clip);clipSettings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,clipSettings);AssetDatabase.CreateAsset(clip,clipPath);
        string idlePath=AssetDatabase.GenerateUniqueAssetPath(folder+"/RealReaperIdle.anim");var idle=new AnimationClip{name="RealReaperIdle",frameRate=100};AnimationUtility.SetObjectReferenceCurve(idle,new EditorCurveBinding{path="Artwork",type=typeof(SpriteRenderer),propertyName="m_Sprite"},new[]{new ObjectReferenceKeyframe{time=0,value=keys[0].value},new ObjectReferenceKeyframe{time=.2f,value=keys[0].value}});AssetDatabase.CreateAsset(idle,idlePath);
        string controllerPath=AssetDatabase.GenerateUniqueAssetPath(folder+"/RealReaperAttack.controller");var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Attack",AnimatorControllerParameterType.Trigger);
        var machine=controller.layers[0].stateMachine;var attack=machine.AddState("Attack");attack.motion=clip;var ready=machine.AddState("Idle");ready.motion=idle;machine.defaultState=attack;
        var finish=attack.AddTransition(ready);finish.hasExitTime=true;finish.exitTime=1;finish.duration=0;var repeat=ready.AddTransition(attack);repeat.hasExitTime=false;repeat.duration=0;repeat.AddCondition(AnimatorConditionMode.If,0,"Attack");
        var root=new GameObject("RealReaperAttackPreview");
        string prefabPath=AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/Minigames/FastingTopDown/RealReaperAttackPreview.prefab");
        try{var art=new GameObject("Artwork");art.transform.SetParent(root.transform,false);var renderer=art.AddComponent<SpriteRenderer>();renderer.sprite=(Sprite)keys[0].value;var material=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minigames/FastingTopDown/TrialObstacles/TrialReaper.prefab").GetComponent<CrossTheLine.Minigames.FastingTopDownHazard>().body.sharedMaterial;renderer.sharedMaterial=material;renderer.sortingOrder=30;root.AddComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();Selection.activeObject=clip;
        return clipPath+"; "+controllerPath+"; "+prefabPath+"; duration="+clip.length+"s, keys="+keys.Length+". Gameplay references unchanged.";
    }
}
