using CrossTheLine;
using UnityEditor;
using UnityEngine;
namespace CrossTheLine.EditorTools
{
 [CustomEditor(typeof(JourneyStageLayout))]
 public sealed class JourneyStageLayoutEditor:Editor
 {
  int selected;
  public override void OnInspectorGUI()
  {
   DrawDefaultInspector();var layout=(JourneyStageLayout)target;
   if(layout.anchors.Count==0)return;
   string[] labels=new string[layout.anchors.Count];for(int i=0;i<labels.Length;i++)labels[i]=layout.anchors[i].key;
   selected=EditorGUILayout.Popup("장면에서 움직일 배치점",Mathf.Clamp(selected,0,labels.Length-1),labels);
   EditorGUILayout.HelpBox("Scene 창의 이동 핸들로 선택한 배치점을 수정합니다. 움직이는 인물의 시작점은 Anchors, 외형·크기는 JourneyEditableVisual, 걷기 프레임은 CharacterView에서 수정합니다.",MessageType.Info);
  }
  void OnSceneGUI()
  {
   var layout=(JourneyStageLayout)target;if(layout.anchors.Count==0)return;selected=Mathf.Clamp(selected,0,layout.anchors.Count-1);var anchor=layout.anchors[selected];
   EditorGUI.BeginChangeCheck();var point=Handles.PositionHandle(anchor.position,Quaternion.identity);
   Handles.Label(point,anchor.key);
   if(EditorGUI.EndChangeCheck()){Undo.RecordObject(layout,"Move chapter placement");anchor.position=point;EditorUtility.SetDirty(layout);PrefabUtility.RecordPrefabInstancePropertyModifications(layout);}
  }
 }
}
