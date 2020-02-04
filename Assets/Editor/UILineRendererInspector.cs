using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using Core;

//===============================================================================
[CustomEditor(typeof(UILineRenderer), true)]
public class UILineRendererInspector : Editor
{
    #region Unity Methods
    //---------------------------------------------------------------------------
    private void OnEnable()
    {
        lineRender = target as UILineRenderer;

        if (serializedObject != null)
        {
            var pointProperty = serializedObject.FindProperty("points");
            reorderList = new ReorderableList(
                serializedObject,
                pointProperty,
                true,
                true,
                true,
                true);

            reorderList.drawElementCallback = OnDrawElementCallback;
            reorderList.onSelectCallback = OnSelectCallBack;
            reorderList.onRemoveCallback = OnRemoveCallBack;
            reorderList.drawHeaderCallback = OnDrawHeader;
        }
    }

    //---------------------------------------------------------------------------
    private void OnSceneGUI()
    {
        if (selectPoint == null)
        {
            return;
        }

        var isTarget = selectPoint.FindPropertyRelative("isTarget");

        if (!isTarget.boolValue)
        {
            var value = selectPoint.FindPropertyRelative("position");
            Transform trans = lineRender.transform;

            if (lineRender.Space == UILineRenderer.PositionType.Absolute)
            {
                value.vector2Value = Handles.PositionHandle(
                    value.vector2Value, Quaternion.identity);
            }
            else
            {
                Vector3 pos = new Vector3(
                    value.vector2Value.x,
                    value.vector2Value.y,
                    0);

                pos = Handles.PositionHandle(
                    pos + trans.position, Quaternion.identity);

                pos -= trans.position;
                value.vector2Value = pos;
            }
        }
        else
        {
            var targetProperty = selectPoint.FindPropertyRelative("target");
            Transform target = targetProperty.objectReferenceValue as Transform;

            if (target != null)
            {
                Vector3 oldPos = target.position;
                target.position = Handles.PositionHandle(target.position,
                    Quaternion.identity);

                if (oldPos != target.position)
                {
                    lineRender.OnRebuildRequested();
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    //---------------------------------------------------------------------------
    public override void OnInspectorGUI()
    {
        if (serializedObject == null)
        {
            return;
        }

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("texture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lineWidth"));

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("posType"));
        bool spaceChanged = EditorGUI.EndChangeCheck();

        //var maring = serializedObject.FindProperty("useMargins");
        //EditorGUILayout.PropertyField(maring);

        //if (maring.boolValue)
        //{
        //    EditorGUILayout.PropertyField(serializedObject.FindProperty("margin"));
        //}

        EditorGUILayout.PropertyField(serializedObject.FindProperty("lineList"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lineCaps"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lineJoin"));

        if (reorderList != null)
        {
            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos,
                GUILayout.MaxHeight(200));
            reorderList.DoLayoutList();
            EditorGUILayout.EndScrollView();
        }

        serializedObject.ApplyModifiedProperties();

        if (spaceChanged)
        {
            OnSpaceChanged();
        }

        if (GUILayout.Button("Refresh"))
        {
            lineRender.OnRebuildRequested();
        }
    }
    #endregion

    #region Internal Methods
    //---------------------------------------------------------------------------
    private void OnDrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, new GUIContent("Points"));
    }

    //---------------------------------------------------------------------------
    private void OnDrawElementCallback(Rect rect,
        int index, bool isActive, bool isFocused)
    {
        var element = reorderList.serializedProperty.GetArrayElementAtIndex(index);

        float typeWidth = 30;
        Rect typeRect = new Rect(
            rect.position.x + rect.width - typeWidth + 5,
            rect.position.y,
            typeWidth - 5,
            rect.height - 5);

        Rect valueRect = new Rect(
            rect.position.x,
            rect.position.y + 1,
            rect.width - typeWidth,
            rect.height - 5
            );

        var isTrans = element.FindPropertyRelative("isTarget");
        //EditorGUI.PropertyField(typeRect, isTrans, GUIContent.none);
        Color oldColor = GUI.color;
        GUI.color = Color.green;
        if (GUI.Button(typeRect, new GUIContent(isTrans.boolValue ? "T" : "P")))
        {
            isTrans.boolValue = !isTrans.boolValue;
        }

        GUI.color = oldColor;

        SerializedProperty valueProperty = null;

        if (!isTrans.boolValue)
        {
            valueProperty = element.FindPropertyRelative("position");
        }
        else
        {
            valueProperty = element.FindPropertyRelative("target");
        }

        if (valueProperty != null)
        {
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
        }
    }

    //---------------------------------------------------------------------------
    private void OnRemoveCallBack(ReorderableList list)
    {
        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        selectPoint = null;
    }

    //---------------------------------------------------------------------------
    private void OnSelectCallBack(ReorderableList list)
    {
        selectPoint = list.serializedProperty.GetArrayElementAtIndex(list.index);
        SceneView.RepaintAll();
    }

    //---------------------------------------------------------------------------
    private void OnSpaceChanged()
    {
        var points = serializedObject.FindProperty("points");

        Vector2 lineRenderPos = (Vector2)lineRender.transform.position;

        for (int i = 0, count = points.arraySize; i < count; i++)
        {
            var point = points.GetArrayElementAtIndex(i);

            var isTrans = point.FindPropertyRelative("isTarget");
            if (isTrans.boolValue)
            {
                continue;
            }

            var value = point.FindPropertyRelative("position");

            if (lineRender.Space == UILineRenderer.PositionType.Absolute)
            {
                value.vector2Value = value.vector2Value + lineRenderPos;
            }
            else
            {
                value.vector2Value = value.vector2Value - lineRenderPos;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
    #endregion

    #region Internal Fields
    //---------------------------------------------------------------------------
    private ReorderableList reorderList;
    private Vector2 scrollViewPos;
    private SerializedProperty selectPoint = null;
    private UILineRenderer lineRender;
    #endregion
}
