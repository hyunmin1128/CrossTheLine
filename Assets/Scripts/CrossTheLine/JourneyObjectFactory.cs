using System;
using System.Collections.Generic;
using UnityEngine;
namespace CrossTheLine
{
 public static class JourneyObjectFactory
 {
  static Dictionary<string,Queue<GameObject>> active;static bool localTemplates;public static bool CaptureMode;public static int AuthoredInstances{get;private set;}
  public static GameObject ActiveStage{get;private set;}
  public static void BeginStage(Component owner,int step)
  {
   active=null;ActiveStage=null;if(CaptureMode)return;
   string[] names={"JordanBaptism","DesertTemptation","FirstDisciples","CanaWedding","TempleCleansing","SynagogueDispute","MiraculousCatch","MatthewCalling","StormAtSea","FeedingMultitude","WaterWalking","PeterConfession","LordsPrayer","ChildrenBlessing","JerusalemEntry","SecondTempleCleansing"};
   int chapter=Array.IndexOf(names,owner.GetType().Name)+1;
   var continuous=owner.GetComponentInParent<JourneyContinuousWorld>();localTemplates=continuous;JourneySceneStages bank=continuous?continuous.ActiveStages:null;
   if(!continuous)foreach(var root in owner.gameObject.scene.GetRootGameObjects()){bank=root.GetComponentInChildren<JourneySceneStages>(true);if(bank)break;}
   var entry=bank?bank.stages.Find(s=>s.chapter==chapter&&s.step==step):null;if(entry==null||!entry.root)return;
   ActiveStage=entry.root;
   active=new Dictionary<string,Queue<GameObject>>();foreach(var t in entry.root.GetComponentsInChildren<Transform>(true)){if(t==entry.root.transform)continue;if(!active.TryGetValue(t.name,out var q))active[t.name]=q=new Queue<GameObject>();q.Enqueue(t.gameObject);}
  }
  public static GameObject Create(string name,params Type[] components)
  {
   GameObject source=null;if(active!=null&&active.TryGetValue(name,out var q)&&q.Count>0)source=q.Dequeue();
   GameObject go;
   if(source){go=UnityEngine.Object.Instantiate(source);if(localTemplates){go.transform.localPosition=source.transform.localPosition;go.transform.localRotation=source.transform.localRotation;go.transform.localScale=source.transform.localScale;}while(go.transform.childCount>0)UnityEngine.Object.DestroyImmediate(go.transform.GetChild(0).gameObject);var visual=go.GetComponent<JourneyEditableVisual>();if(visual)visual.BindTemplate(source);go.SetActive(true);AuthoredInstances++;}
   else{go=new GameObject();}
   go.name=name;foreach(var type in components)if(!go.GetComponent(type))go.AddComponent(type);return go;
  }
  public static T GetOrAddComponent<T>(this GameObject go)where T:Component{var existing=go.GetComponent<T>();return existing?existing:go.AddComponent<T>();}
  public static void Reset(){active=null;ActiveStage=null;localTemplates=false;AuthoredInstances=0;}
 }
}

