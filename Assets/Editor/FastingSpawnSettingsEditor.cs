using UnityEditor;
using UnityEngine;
using CrossTheLine.Minigames;
[CustomEditor(typeof(FastingSpawnSettings))]
public sealed class FastingSpawnSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("전체 빈도 × 스테이지 빈도: 1=현재, 2=두 배 자주, 0=중지. 비중은 합계 대비 %로 자동 환산됩니다. 0인 장애물은 랜덤·패턴 모두 제외됩니다. 묶음 개수와 통로 보호 때문에 실제 등장 개수 비율은 다를 수 있습니다.",MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("globalFrequency"),new GUIContent("전체 장애물 생성 빈도"));
        var stages=serializedObject.FindProperty("stages");
        for(int i=0;i<stages.arraySize;i++){
            var stage=stages.GetArrayElementAtIndex(i);EditorGUILayout.Space();EditorGUILayout.LabelField((i+1)+"스테이지 · "+stage.FindPropertyRelative("name").stringValue,EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stage.FindPropertyRelative("frequency"),new GUIContent("이 스테이지 생성 빈도"));
            var entries=stage.FindPropertyRelative("obstacles");float total=0;
            for(int j=0;j<entries.arraySize;j++)total+=Mathf.Max(0,entries.GetArrayElementAtIndex(j).FindPropertyRelative("weight").floatValue);
            for(int j=0;j<entries.arraySize;j++){
                var entry=entries.GetArrayElementAtIndex(j);var weight=entry.FindPropertyRelative("weight");var kind=(FastingTopDownHazard.Kind)entry.FindPropertyRelative("kind").enumValueIndex;
                EditorGUILayout.BeginHorizontal();EditorGUILayout.PropertyField(weight,new GUIContent(i==1&&kind==FastingTopDownHazard.Kind.Rock?"석순":i==2&&kind==FastingTopDownHazard.Kind.Rock?"무너진 잔해":FastingTopDownGame.ObstacleName(kind)));EditorGUILayout.LabelField(total>0?(Mathf.Max(0,weight.floatValue)/total*100).ToString("0.#")+"%":"0%",GUILayout.Width(52));EditorGUILayout.EndHorizontal();
            }
            if(total<=0)EditorGUILayout.HelpBox("모든 비중이 0이므로 이 스테이지 장애물은 생성되지 않습니다.",MessageType.Warning);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
