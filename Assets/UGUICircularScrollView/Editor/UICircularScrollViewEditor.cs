using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace CircularScrollView
{

    [CustomEditor(typeof(CircularScrollView.UICircularScrollView))]
    public class UICircularScrollViewEditor : Editor
    {
        //private UICircularScrollView t;
        SerializedProperty topPadding;
        SerializedProperty eDirection;
        SerializedProperty m_Row;
        SerializedProperty m_Spacing;
        SerializedProperty m_CellGameObject;
        SerializedProperty ptrRowBg;
        SerializedProperty itemRowBg;

        UICircularScrollView list;
        protected void OnEnable()
        {
            topPadding = serializedObject.FindProperty("topPadding");//Vector2
            eDirection = serializedObject.FindProperty("m_Direction");//e_Direction
            m_Row = serializedObject.FindProperty("m_Row");//int
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_CellGameObject = serializedObject.FindProperty("m_CellGameObject");
            ptrRowBg = serializedObject.FindProperty("ptrRowBg");
            itemRowBg = serializedObject.FindProperty("itemRowBg");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //DrawDefaultInspector();
            //base.OnInspectorGUI();
            list = (CircularScrollView.UICircularScrollView)target;

            eDirection.enumValueIndex = Convert.ToInt32(EditorGUILayout.EnumPopup("Direction: ", (e_Direction)eDirection.intValue));

            //list.m_Row = EditorGUILayout.IntField("Row Or Column: ", list.m_Row);
            m_Row.intValue = EditorGUILayout.IntField("Row Or Column: ", m_Row.intValue);
            m_Spacing.floatValue = EditorGUILayout.FloatField("Spacing: ", m_Spacing.floatValue);
            m_CellGameObject.objectReferenceValue = EditorGUILayout.ObjectField("Cell: ", m_CellGameObject.objectReferenceValue, typeof(GameObject), true);
            //list.m_Spacing = EditorGUILayout.FloatField("Spacing: ", list.m_Spacing);
            //list.m_CellGameObject = (GameObject)EditorGUILayout.ObjectField("Cell: ", list.m_CellGameObject, typeof(GameObject), true);
            //list.topPadding = EditorGUILayout.Vector2Field("TopPadding: ", list.topPadding);
            //EditorGUILayout.PropertyField(topPadding, new GUIContent("TopPadding"),null);
            topPadding.vector2Value = EditorGUILayout.Vector2Field("TopPadding",topPadding.vector2Value, null);
            ptrRowBg.objectReferenceValue = EditorGUILayout.ObjectField("PtrRowBg: ", ptrRowBg.objectReferenceValue, typeof(Transform), true);
            itemRowBg.objectReferenceValue = EditorGUILayout.ObjectField("ItemRowBg: ", itemRowBg.objectReferenceValue, typeof(GameObject), true);

            list.m_IsShowArrow = EditorGUILayout.ToggleLeft("IsShowArrow", list.m_IsShowArrow);
            if (list.m_IsShowArrow)
            {
                list.m_PointingFirstArrow = (GameObject)EditorGUILayout.ObjectField("Up or Left Arrow: ", list.m_PointingFirstArrow, typeof(GameObject), true);
                list.m_PointingEndArrow = (GameObject)EditorGUILayout.ObjectField("Down or Right Arrow: ", list.m_PointingEndArrow, typeof(GameObject), true);
            }
            

            serializedObject.ApplyModifiedProperties();
        }


    }
}
