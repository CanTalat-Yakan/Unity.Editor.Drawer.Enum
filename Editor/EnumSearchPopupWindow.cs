#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public class EnumSearchPopup : EditorWindow
    {
        private const float LINE_HEIGHT = 20f;
        private const float PADDING = 4f;
        private const float MIN_WINDOW_WIDTH = 250f;
        private const float MAX_WINDOW_HEIGHT = 600f;

        private string searchString = "";
        private Vector2 scrollPosition;
        private string[] enumNames;
        private Array enumValues;
        private Type enumType;
        private UnityEngine.Object targetObject;
        private string propertyPath;
        private int currentIndex;

        public static void Show(Rect buttonRect, UnityEngine.Object target, string path,
                              Type type, Enum currentValue, string[] names)
        {
            EnumSearchPopup window = CreateInstance<EnumSearchPopup>();
            window.targetObject = target;
            window.propertyPath = path;
            window.enumType = type;
            window.enumValues = Enum.GetValues(type);
            window.enumNames = names;
            window.currentIndex = Array.IndexOf(window.enumValues, currentValue);
            window.searchString = "";

            Vector2 windowPos = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            float availableHeight = Screen.currentResolution.height - windowPos.y - 30f;

            // Calculate dynamic window size
            float contentHeight = Mathf.Min(
                window.CalculateContentHeight(),
                MAX_WINDOW_HEIGHT,
                availableHeight
            );

            window.position = new Rect(
                windowPos.x - PADDING,
                windowPos.y,
                Mathf.Max(buttonRect.width + PADDING * 2, MIN_WINDOW_WIDTH),
                contentHeight + PADDING * 2
            );

            window.ShowPopup();
            window.Focus();
        }

        private float CalculateContentHeight()
        {
            int itemCount = GetFilteredIndices().Count;
            return LINE_HEIGHT + // Search field height
                   (itemCount * LINE_HEIGHT) + // Items height
                   PADDING * 4; // Additional padding
        }

        void OnGUI()
        {
            HandleKeyboard();
            DrawSearchField();
            DrawEnumList();
        }

        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                }
                else if (Event.current.keyCode == KeyCode.Return &&
                         GetFilteredIndices().Count == 1)
                {
                    SetEnumValue(GetFilteredIndices()[0]);
                    Close();
                }
            }
        }

        private void DrawSearchField()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(PADDING);
            GUI.SetNextControlName("SearchField");
            searchString = GUILayout.TextField(searchString, EditorStyles.toolbarSearchField,
                GUILayout.Height(LINE_HEIGHT));
            GUILayout.Space(PADDING);
            GUILayout.EndHorizontal();
            EditorGUI.FocusTextInControl("SearchField");
        }

        private void DrawEnumList()
        {
            List<int> filteredIndices = GetFilteredIndices();

            GUILayout.Space(PADDING);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition,
                GUILayout.ExpandWidth(true));

            foreach (int index in filteredIndices)
            {
                DrawEnumButton(index);
            }

            GUILayout.EndScrollView();
            GUILayout.Space(PADDING);
        }

        private List<int> GetFilteredIndices()
        {
            string lowerSearch = searchString.ToLower();
            List<int> indices = new List<int>();

            for (int i = 0; i < enumNames.Length; i++)
            {
                if (string.IsNullOrEmpty(searchString) ||
                    enumNames[i].ToLower().Contains(lowerSearch))
                {
                    indices.Add(i);
                }
            }
            return indices;
        }

        private void DrawEnumButton(int index)
        {
            Rect rect = EditorGUILayout.GetControlRect(
                GUILayout.Height(LINE_HEIGHT),
                GUILayout.ExpandWidth(true)
            );

            if (Event.current.type == EventType.Repaint)
            {
                bool isSelected = index == currentIndex;
                var style = isSelected ?
                    EditorStyles.miniButton :
                    EditorStyles.miniButton;

                style.Draw(rect, GUIContent.none, false, false, isSelected, false);
            }

            Rect labelRect = rect;
            labelRect.xMin += PADDING * 2;
            labelRect.xMax -= PADDING * 2;

            GUI.Label(labelRect, enumNames[index], GetLabelStyle(index == currentIndex));

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                SetEnumValue(index);
                Close();
            }
        }

        private GUIStyle GetLabelStyle(bool isSelected)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                padding = new RectOffset((int)(PADDING * 2), 0, 0, 0)
            };
            return style;
        }

        private void SetEnumValue(int index)
        {
            SerializedObject so = new SerializedObject(targetObject);
            SerializedProperty prop = so.FindProperty(propertyPath);
            if (prop != null && prop.enumValueIndex != index)
            {
                prop.enumValueIndex = index;
                so.ApplyModifiedProperties();
            }
        }

        void OnLostFocus()
        {
            Close();
        }
    }

}
#endif