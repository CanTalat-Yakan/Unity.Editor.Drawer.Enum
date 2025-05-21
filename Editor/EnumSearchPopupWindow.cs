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
        private const float MinWndowWidth = 250f;
        private const float MaxWindowHeight = 1200f;

        private string _searchString = "";
        private Vector2 _scrollPosition;
        private string[] _enumNames;
        private Array _enumValues;
        private Type _enumType;
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
            window._enumType = enumType;
            window._enumValues = Enum.GetValues(enumType);
            window._enumNames = Enum.GetNames(enumType);
            window._currentIndex = Array.IndexOf(window._enumValues, currentValue);
            window._onValueSelected = onValueSelected;
            window._searchString = "";
            window._hoverIndex = window._currentIndex;

            Vector2 windowPosition = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            float availableHeight = Screen.currentResolution.height - windowPosition.y - 30f;

            float contentHeight = Mathf.Min(
                window.CalculateContentHeight(),
                MaxWindowHeight,
                availableHeight);

            window.position = new Rect(
                windowPosition.x - Padding,
                windowPosition.y,
                Mathf.Max(buttonRect.width + Padding * 2, MinWndowWidth),
                contentHeight + Padding * 2);

            Vector2 windowSize = new Vector2(Mathf.Max(buttonRect.width, MinWndowWidth), contentHeight);
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
                var current = filtered.IndexOf(_hoverIndex);

                switch (Event.current.keyCode)
                {
                    case KeyCode.DownArrow:
                        _hoverIndex = filtered[Mathf.Clamp(current + 1, 0, filtered.Count - 1)];
                        Event.current.Use();
                        ScrollToItem(_hoverIndex);
                        break;

                    case KeyCode.UpArrow:
                        _hoverIndex = filtered[Mathf.Clamp(current - 1, 0, filtered.Count - 1)];
                        Event.current.Use();
                        ScrollToItem(_hoverIndex);
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

        private void HandleMouseMovement()
        {
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.Layout)
            {
                var mousePosition = Event.current.mousePosition;
                var contentPosition = new Rect(0, LineHeight, position.width, position.height - (LineHeight));
                if (contentPosition.Contains(mousePosition))
                {
                    var filtered = GetFilteredIndices();
                    float scrollY = mousePosition.y - LineHeight + _scrollPosition.y;
                    int itemIndex = Mathf.FloorToInt(scrollY / LineHeight);
                    _hoverIndex = itemIndex >= 0 && itemIndex < filtered.Count ? filtered[itemIndex] : -1;
                    Repaint();
                }
                else if (_hoverIndex != -1)
                {
                    _hoverIndex = -1;
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
            var pos = filtered.IndexOf(index) * LineHeight;
            _scrollPosition.y = Mathf.Clamp(pos - position.height / 2, 0, pos);
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
                    bool isSelected = index == _currentIndex;
                    bool isHovered = index == _hoverIndex;
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
                padding = new RectOffset(10, 0, 0, 0)
            };

            style.normal.textColor = highlighted ? Color.white : EditorStyles.label.normal.textColor;
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
                   Padding * 4; // Additional padding
        }

        private List<int> GetFilteredIndices()
        {
            var lowerSearch = _searchString.ToLower();
            var indices = new List<int>();

            for (int i = 0; i < _enumNames.Length; i++)
                if (string.IsNullOrEmpty(_searchString) || _enumNames[i].ToLower().Contains(lowerSearch))
                    indices.Add(i);

            return indices;
        }

        private void SetEnumValue(int index)
        {
            Enum selectedValue = (Enum)_enumValues.GetValue(index);
            _onValueSelected?.Invoke(selectedValue);
            Close();
        }
    }
}
#endif