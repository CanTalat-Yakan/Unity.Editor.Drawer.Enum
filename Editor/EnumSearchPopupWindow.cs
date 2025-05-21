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
        private int currentIndex;
        private int hoverIndex = -1;
        private Action<Enum> onValueSelected;

        private static readonly Color borderColorPro = new Color(0.11f, 0.11f, 0.11f);
        private static readonly Color borderColorLight = new Color(0.51f, 0.51f, 0.51f);
        private static readonly Color highlightColorPro = new Color(0.24f, 0.48f, 0.9f);
        private static readonly Color highlightColorLight = new Color(0.22f, 0.44f, 0.9f);

        private bool hasInitializedScrollPosition;

        public static void Show(Rect buttonRect, Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            var window = CreateInstance<EnumSearchPopup>();
            window.enumType = enumType;
            window.enumValues = Enum.GetValues(enumType);
            window.enumNames = Enum.GetNames(enumType);
            window.currentIndex = Array.IndexOf(window.enumValues, currentValue);
            window.onValueSelected = onValueSelected;
            window.searchString = "";
            window.hoverIndex = window.currentIndex;

            Vector2 windowPosition = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            float availableHeight = Screen.currentResolution.height - windowPosition.y - 30f;

            float contentHeight = Mathf.Min(
                window.CalculateContentHeight(),
                MAX_WINDOW_HEIGHT,
                availableHeight);

            window.position = new Rect(
                windowPosition.x - PADDING,
                windowPosition.y,
                Mathf.Max(buttonRect.width + PADDING * 2, MIN_WINDOW_WIDTH),
                contentHeight + PADDING * 2);

            Vector2 windowSize = new Vector2(Mathf.Max(buttonRect.width, MIN_WINDOW_WIDTH), contentHeight);
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), windowSize);
            window.Focus();
        }

        public void OnGUI()
        {
            HandleKeyboard();
            HandleMouseMovement();

            DrawBorder(() =>
            {
                DrawSearchField();
                DrawEnumList();
            });

            if (!hasInitializedScrollPosition)
            {
                ScrollToCurrentItem();
                hasInitializedScrollPosition = true;
            }
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

        private void HandleMouseMovement()
        {
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.Layout)
            {
                var mousePosition = Event.current.mousePosition;
                var contentPosition = new Rect(0, LINE_HEIGHT + PADDING, position.width, position.height - (LINE_HEIGHT + PADDING));
                if (contentPosition.Contains(mousePosition))
                {
                    var filtered = GetFilteredIndices();
                    float scrollY = mousePosition.y - LINE_HEIGHT + scrollPosition.y;
                    int itemIndex = Mathf.FloorToInt(scrollY / LINE_HEIGHT);
                    hoverIndex = itemIndex >= 0 && itemIndex < filtered.Count ? filtered[itemIndex] : -1;
                    Repaint();
                }
                else if (hoverIndex != -1)
                {
                    hoverIndex = -1;
                    Repaint();
                }
            }
        }

        private void ScrollToCurrentItem()
        {
            var filtered = GetFilteredIndices();
            if (filtered.Contains(currentIndex))
            {
                hoverIndex = currentIndex;
                ScrollToItem(currentIndex);
            }
        }

        private void ScrollToItem(int index)
        {
            var filtered = GetFilteredIndices();
            var pos = filtered.IndexOf(index) * LINE_HEIGHT;
            scrollPosition.y = Mathf.Clamp(pos - position.height / 2, 0, pos);
        }

        private void DrawBorder(Action drawContent)
        {
            var borderColor = EditorGUIUtility.isProSkin ? borderColorPro : borderColorLight;
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), borderColor);

            var backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
            EditorGUI.DrawRect(new Rect(1, 1, position.width - 2, position.height - 2), backgroundColor);

            GUILayout.BeginArea(new Rect(1, 1, position.width - 2, position.height - 2));
            drawContent();
            GUILayout.EndArea();
        }

        private void DrawEnumList()
        {
            var filteredIndices = GetFilteredIndices();
            var listHeight = filteredIndices.Count * LINE_HEIGHT;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            for (int i = 0; i < filteredIndices.Count; i++)
            {
                var index = filteredIndices[i];
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(LINE_HEIGHT));

                if (Event.current.type == EventType.Repaint)
                {
                    bool isSelected = index == currentIndex;
                    bool isHovered = index == hoverIndex;
                    DrawItemBackground(rect, isSelected, isHovered);
                    DrawItemText(rect, enumNames[index], isSelected || isHovered);
                }

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    SetEnumValue(index);
                    Close();
                }
            }

            GUILayout.EndScrollView();
        }
        private void DrawItemBackground(Rect rect, bool isSelected, bool isHovered)
        {
            if (isSelected || isHovered)
            {
                var color = EditorGUIUtility.isProSkin ? highlightColorPro : highlightColorLight;
                EditorGUI.DrawRect(rect, color);
            }
        }

        private void DrawItemText(Rect rect, string text, bool highlighted)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0)
            };

            style.normal.textColor = highlighted ? Color.white : EditorStyles.label.normal.textColor;
            GUI.Label(rect, ObjectNames.NicifyVariableName(text), style);
        }

        private void DrawSearchField()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("SearchField");
            searchString = GUILayout.TextField(searchString, EditorStyles.toolbarSearchField);
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
            Enum selectedValue = (Enum)enumValues.GetValue(index);
            onValueSelected?.Invoke(selectedValue);
            Close();
        }
    }
}
#endif