#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public class EnumSearchPopup : EditorWindow
    {
        private const float LINE_HEIGHT = 22f;
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
        private int hoverIndex = -1;

        public static void Show(Rect buttonRect, UnityEngine.Object target, string path, Type type, Enum currentValue, string[] names)
        {
            var window = CreateInstance<EnumSearchPopup>();
            window.targetObject = target;
            window.propertyPath = path;
            window.enumType = type;
            window.enumValues = Enum.GetValues(type);
            window.enumNames = names;
            window.currentIndex = Array.IndexOf(window.enumValues, currentValue);
            window.searchString = "";
            window.hoverIndex = window.currentIndex;

            var windowPos = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            var availableHeight = Screen.currentResolution.height - windowPos.y - 30f;

            var contentHeight = Mathf.Min(
                window.CalculateContentHeight(),
                MAX_WINDOW_HEIGHT,
                availableHeight);

            window.position = new Rect(
                windowPos.x - PADDING,
                windowPos.y,
                Mathf.Max(buttonRect.width + PADDING * 2, MIN_WINDOW_WIDTH),
                contentHeight + PADDING * 2);

            var windowSize = new Vector2( Mathf.Max(buttonRect.width, MIN_WINDOW_WIDTH), contentHeight);
            window.ShowAsDropDown( GUIUtility.GUIToScreenRect(buttonRect), windowSize);
            window.Focus();
        }

        public void OnGUI()
        {
            HandleKeyboard();
            DrawSearchField();
            DrawEnumList();

            if (Event.current.type == EventType.MouseMove)
                Repaint();
        }

        public void OnLostFocus() =>
            Close();

        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                var filtered = GetFilteredIndices();
                var current = filtered.IndexOf(hoverIndex);

                switch (Event.current.keyCode)
                {
                    case KeyCode.DownArrow:
                        hoverIndex = filtered[Mathf.Clamp(current + 1, 0, filtered.Count - 1)];
                        Event.current.Use();
                        ScrollToItem(hoverIndex);
                        break;

                    case KeyCode.UpArrow:
                        hoverIndex = filtered[Mathf.Clamp(current - 1, 0, filtered.Count - 1)];
                        Event.current.Use();
                        ScrollToItem(hoverIndex);
                        break;

                    case KeyCode.Return when filtered.Count > 0:
                        SetEnumValue(hoverIndex);
                        Close();
                        break;

                    case KeyCode.Escape:
                        Close();
                        break;
                }
            }
        }

        private void ScrollToItem(int index)
        {
            var filtered = GetFilteredIndices();
            var pos = filtered.IndexOf(index) * LINE_HEIGHT;
            scrollPosition.y = Mathf.Clamp(pos - position.height / 2, 0, pos);
        }

        private void DrawEnumList()
        {
            var filteredIndices = GetFilteredIndices();

            // Background color
            var listPosition = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(listPosition, EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f));

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (int index in filteredIndices)
                DrawEnumLabel(index);

            GUILayout.EndScrollView();
        }

        private void DrawEnumLabel(int index)
        {
            var position = EditorGUILayout.GetControlRect(
                GUILayout.Height(LINE_HEIGHT),
                GUILayout.ExpandWidth(true));

            var isSelected = index == currentIndex;
            var isHovered = position.Contains(Event.current.mousePosition) || index == hoverIndex;

            // Handle mouse interaction
            if (Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                SetEnumValue(index);
                Close();
            }

            if (Event.current.type == EventType.Repaint)
            {
                var highlightColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.48f, 0.9f)
                    : new Color(0.22f, 0.44f, 0.9f);

                if (isHovered || isSelected)
                    EditorGUI.DrawRect(position, highlightColor);

                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 0, 0, 0),
                    normal = { textColor = isHovered || isSelected ? Color.white : EditorStyles.label.normal.textColor }
                };

                GUI.Label(position, ObjectNames.NicifyVariableName(enumNames[index]), labelStyle);
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

        private float CalculateContentHeight()
        {
            var itemCount = GetFilteredIndices().Count;
            return LINE_HEIGHT + // Search field height
                   (itemCount * LINE_HEIGHT) + // Items height
                   PADDING * 4; // Additional padding
        }

        private List<int> GetFilteredIndices()
        {
            var lowerSearch = searchString.ToLower();
            var indices = new List<int>();

            for (int i = 0; i < enumNames.Length; i++)
                if (string.IsNullOrEmpty(searchString) || enumNames[i].ToLower().Contains(lowerSearch))
                    indices.Add(i);

            return indices;
        }

        private void SetEnumValue(int index)
        {
            var serializedObject = new SerializedObject(targetObject);
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null && property.enumValueIndex != index)
            {
                property.enumValueIndex = index;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif