#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public class EnumSearchPopup : EditorWindow
    {
        private const float LineHeight = 22f;
        private const float Padding = 4f;
        private const float MinWindowWidth = 150f;
        private const float MaxWindowHeight = 1500f;

        private string _searchString = string.Empty;
        private Vector2 _scrollPosition;
        private string[] _enumNames;
        private Array _enumValues;
        private int _currentIndex;
        private int _hoverIndex = -1;
        private Action<Enum> _onValueSelected;

        private static readonly Color s_borderColorPro = new Color(0.11f, 0.11f, 0.11f);
        private static readonly Color s_borderColorLight = new Color(0.51f, 0.51f, 0.51f);
        private static readonly Color s_backgroundColorPro = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color s_backgroundColorLight = new Color(0.76f, 0.76f, 0.76f);
        private static readonly Color s_highlightColorPro = new Color(0.24f, 0.37f, 0.58f);
        private static readonly Color s_highlightColorLight = new Color(0.22f, 0.44f, 0.9f);

        private bool hasInitializedScrollPosition;

        public static void Show(Rect buttonRect, Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            var window = CreateInstance<EnumSearchPopup>();
            window._enumValues = Enum.GetValues(enumType);
            window._enumNames = Enum.GetNames(enumType);
            window._currentIndex = Array.IndexOf(window._enumValues, currentValue);
            window._onValueSelected = onValueSelected;
            window._searchString = string.Empty;
            window._hoverIndex = window._currentIndex;

            Vector2 windowPosition = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            float availableHeight = Screen.currentResolution.height - windowPosition.y;

            float contentHeight = Mathf.Min(
                window.CalculateContentHeight(),
                MaxWindowHeight,
                availableHeight);

            window.position = new Rect(
                windowPosition.x - Padding,
                windowPosition.y,
                Mathf.Max(buttonRect.width + Padding * 2, MinWindowWidth),
                contentHeight + Padding * 2);

            Vector2 windowSize = new Vector2(Mathf.Max(buttonRect.width, MinWindowWidth), contentHeight);
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), windowSize);
            window.Focus();
            window.ScrollToCurrentItem();
        }

        public void OnGUI()
        {
            string previousSearch = _searchString;
            HandleKeyboard();
            HandleMouseMovement();

            DrawBorder(() =>
            {
                DrawSearchField();
                DrawEnumList();
            });

            // Reset hover index when search changes
            if (_searchString != previousSearch)
            {
                var filtered = GetFilteredIndices();
                _hoverIndex = filtered.Count > 0 ? filtered[0] : -1;
                if (_hoverIndex != -1)
                    ScrollToItem(_hoverIndex);
            }
        }

        public void OnLostFocus() =>
            Close();

        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                var filtered = GetFilteredIndices();
                if (filtered.Count == 0)
                    return;

                var current = filtered.IndexOf(_hoverIndex);
                current = Mathf.Clamp(current, 0, filtered.Count - 1);

                switch (Event.current.keyCode)
                {
                    case KeyCode.DownArrow:
                        current = Mathf.Clamp(current + 1, 0, filtered.Count - 1);
                        _hoverIndex = filtered[current];
                        Event.current.Use();
                        ScrollToItem(_hoverIndex);
                        Repaint();
                        break;

                    case KeyCode.UpArrow:
                        current = Mathf.Clamp(current - 1, 0, filtered.Count - 1);
                        _hoverIndex = filtered[current];
                        Event.current.Use();
                        ScrollToItem(_hoverIndex);
                        Repaint();
                        break;

                    case KeyCode.Return when filtered.Count > 0:
                        SetEnumValue(_hoverIndex);
                        Close();
                        break;

                    case KeyCode.Escape:
                        Close();
                        break;
                }
            }
        }

        private Vector2 _previousMousePosition;
        private void HandleMouseMovement()
        {
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.Layout)
            {
                if (_previousMousePosition == Event.current.mousePosition)
                    return;
                _previousMousePosition = Event.current.mousePosition;

                var contentPosition = new Rect(0, LineHeight, position.width, position.height - (LineHeight));
                if (contentPosition.Contains(Event.current.mousePosition))
                {
                    var filtered = GetFilteredIndices();
                    var scrollY = _previousMousePosition.y - LineHeight + _scrollPosition.y;
                    var itemIndex = Mathf.FloorToInt(scrollY / LineHeight);
                    _hoverIndex = itemIndex >= 0 && itemIndex < filtered.Count ? filtered[itemIndex] : -1;
                    Repaint();
                }
            }
        }

        private void ScrollToCurrentItem()
        {
            var filtered = GetFilteredIndices();
            if (filtered.Contains(_currentIndex))
            {
                _hoverIndex = _currentIndex;
                ScrollToItem(_currentIndex);
            }
        }

        private void ScrollToItem(int index)
        {
            var filtered = GetFilteredIndices();
            var itemIndex = filtered.IndexOf(index);
            if (itemIndex == -1) return;

            var itemPosition = itemIndex * LineHeight;
            var scrollViewHeight = position.height - LineHeight - 2 * Padding; // Account for search field and padding
            var maxScroll = Mathf.Max(0, (filtered.Count * LineHeight) - scrollViewHeight);

            // Center the item in the scroll view
            _scrollPosition.y = Mathf.Clamp(itemPosition - (scrollViewHeight / 2), 0, maxScroll);
        }

        private void DrawBorder(Action drawContent)
        {
            var borderColor = EditorGUIUtility.isProSkin ? s_borderColorPro : s_borderColorLight;
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), borderColor);

            var backgroundColor = EditorGUIUtility.isProSkin ? s_backgroundColorPro : s_backgroundColorLight;
            EditorGUI.DrawRect(new Rect(1, 1, position.width - 2, position.height - 2), backgroundColor);

            GUILayout.BeginArea(new Rect(1, 1, position.width - 2, position.height - 2));
            drawContent();
            GUILayout.EndArea();
        }

        private void DrawEnumList()
        {
            var filteredIndices = GetFilteredIndices();
            var listHeight = filteredIndices.Count * LineHeight;

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            for (int i = 0; i < filteredIndices.Count; i++)
            {
                var index = filteredIndices[i];
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(LineHeight));

                if (Event.current.type == EventType.Repaint)
                {
                    var isSelected = index == _currentIndex;
                    var isHovered = index == _hoverIndex;
                    DrawItemBackground(rect, isSelected || isHovered);
                    DrawItemText(rect, _enumNames[index], isSelected || isHovered);
                }

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    SetEnumValue(index);
                    Close();
                }
            }

            GUILayout.EndScrollView();
        }
        private void DrawItemBackground(Rect rect, bool highlighted)
        {
            if (highlighted)
            {
                var color = EditorGUIUtility.isProSkin ? s_highlightColorPro : s_highlightColorLight;
                EditorGUI.DrawRect(rect, color);
            }
        }

        private void DrawItemText(Rect rect, string text, bool highlighted)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0),
                normal = { textColor = highlighted ? Color.white : EditorStyles.label.normal.textColor },
                hover = { textColor = highlighted ? Color.white : EditorStyles.label.normal.textColor }
            };

            GUI.Label(rect, ObjectNames.NicifyVariableName(text), style);
        }

        private void DrawSearchField()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("SearchField");
            _searchString = GUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();
            EditorGUI.FocusTextInControl("SearchField");
        }

        private float CalculateContentHeight()
        {
            var itemCount = GetFilteredIndices().Count;
            return LineHeight + // Search field height
                   (itemCount * LineHeight) + // Items height
                   1; // Additional padding
        }

        private List<int> GetFilteredIndices()
        {
            var lowerSearch = _searchString.ToLower();
            var indices = new List<int>();

            for (int i = 0; i < _enumNames.Length; i++)
            {
                var rawName = _enumNames[i];
                var nicifiedName = ObjectNames.NicifyVariableName(rawName);

                bool matchesRaw = rawName.ToLower().Contains(lowerSearch);
                bool matchesNicified = nicifiedName.ToLower().Contains(lowerSearch);

                if (string.IsNullOrEmpty(lowerSearch) || matchesRaw || matchesNicified)
                    indices.Add(i);
            }

            return indices;
        }

        private void SetEnumValue(int index)
        {
            try
            {
                Enum selectedValue = (Enum)_enumValues.GetValue(index);
                _onValueSelected?.Invoke(selectedValue);
                Close();
            }
            catch (Exception) { }
        }
    }
}
#endif