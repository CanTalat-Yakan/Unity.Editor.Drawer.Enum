#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a popup window for selecting an enumeration value with search and filtering capabilities.
    /// </summary>
    /// <remarks>This class is designed to display a dropdown-style popup for selecting an enumeration value.
    /// It supports searching and filtering through the enumeration values, as well as keyboard and mouse navigation.
    /// The popup is used in editor contexts where a user-friendly interface for selecting enum values is
    /// required.</remarks>
    public class EnumEditor
    {
        public EditorWindowDrawer window;
        public Action Repaint;
        public Action Close;

        private string[] _enumNames;
        private Array _enumValues;
        private int _currentIndex;
        private string _currentSearchString = string.Empty;
        private string _previousSearchString = string.Empty;
        private int _hoverIndex = -1;
        private Action<Enum> _onValueSelected;

        private const float LineHeight = 22f;
        private const float Padding = 4f;
        private const float MinWindowWidth = 75f;
        private const float MaxWindowHeight = 1500f;

        public EnumEditor(Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            _enumValues = Enum.GetValues(enumType);
            _enumNames = Enum.GetNames(enumType);
            _currentIndex = Array.IndexOf(_enumValues, currentValue);
            _hoverIndex = _currentIndex;
            _onValueSelected = onValueSelected;
        }

        public static void ShowAsDropDown(Rect buttonRect, Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            var editor = new EnumEditor(enumType, currentValue, onValueSelected);

            var windowPosition = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            var availableHeight = Screen.currentResolution.height - windowPosition.y;
            var contentwidth = Mathf.Max(MinWindowWidth, buttonRect.width);
            var contentHeight = Mathf.Min(MaxWindowHeight, availableHeight, editor.CalculateContentHeight());
            var dropdownSize = new Vector2(contentwidth, contentHeight);

            editor.window = new EditorWindowDrawer() 
                .SetPreProcess(editor.PreProcess)
                .SetPostProcess(editor.PostProcess)
                .SetHeader(editor.Header)
                .SetBody(editor.Body)
                .GetRepaintEvent(out editor.Repaint)
                .GetCloseEvent(out editor.Close)
                .SetDrawBorder()
                .ShowAsDropDown(buttonRect, dropdownSize);

            editor.ScrollToCurrentItem();
        }

        public void PreProcess()
        {
            _previousSearchString = _currentSearchString;
            HandleKeyboardInput();
            HandleMouseMovement();
        }

        public void PostProcess()
        {
            if (_currentSearchString != _previousSearchString)
            {
                var filtered = GetFilteredIndices();
                _hoverIndex = filtered.Count > 0 ? filtered[0] : -1;
                if (_hoverIndex != -1)
                    ScrollToItem(_hoverIndex);
            }
        }

        public void Header()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("SearchField");
            _currentSearchString = GUILayout.TextField(_currentSearchString, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();
            EditorGUI.FocusTextInControl("SearchField");
        }

        public void Body()
        {
            var filteredIndices = GetFilteredIndices();
            var listHeight = filteredIndices.Count * LineHeight;

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
        }

        private void HandleKeyboardInput()
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

                    case KeyCode.Return:
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

                var contentPosition = new Rect(0, LineHeight, window.Position.width, window.Position.height - (LineHeight));
                if (contentPosition.Contains(Event.current.mousePosition))
                {
                    var filtered = GetFilteredIndices();
                    var scrollY = _previousMousePosition.y - LineHeight + window.ScrollPosition.y;
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
            if (itemIndex == -1)
                return;

            var itemPosition = itemIndex * LineHeight;
            var scrollViewHeight = window.Position.height - LineHeight - 2 * Padding; // Account for search field and padding
            var maxScroll = Mathf.Max(0, (filtered.Count * LineHeight) - scrollViewHeight);

            // Center the item in the scroll view
            window.ScrollPosition.y = Mathf.Clamp(itemPosition - (scrollViewHeight / 2), 0, maxScroll);
        }

        private static readonly Color s_highlightColorPro = new Color(0.24f, 0.37f, 0.58f);
        private static readonly Color s_highlightColorLight = new Color(0.22f, 0.44f, 0.9f);
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
            var color = highlighted ? Color.white : EditorStyles.label.normal.textColor;
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0),
                normal = { textColor = color },
                hover = { textColor = color }
            };

            GUI.Label(rect, ObjectNames.NicifyVariableName(text), style);
        }

        /// <summary>
        /// Calculates the total height of the content based on the line height and the number of filtered indices.
        /// </summary>
        /// <returns>The total height of the content as a floating-point value. This includes the base line height,  the height
        /// contributed by the filtered indices, and an additional offset of 1.</returns>
        private float CalculateContentHeight() =>
            LineHeight + (GetFilteredIndices().Count * LineHeight) + 1;

        /// <summary>
        /// Retrieves a list of indices from the enumeration names that match the current search string.
        /// </summary>
        /// <remarks>The method compares the search string against both the raw and nicified versions of
        /// the enumeration names. A match is determined if the search string is a substring of either version, ignoring
        /// case. If the search string is empty or null, all indices are included in the result.</remarks>
        /// <returns>A list of indices corresponding to enumeration names that match the search string.  The list will contain
        /// all indices if the search string is empty or null.</returns>
        private List<int> GetFilteredIndices()
        {
            var lowerSearch = _currentSearchString.ToLower();
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

        /// <summary>
        /// Sets the selected enumeration value based on the specified index and invokes the associated callback.
        /// </summary>
        /// <remarks>This method retrieves the enumeration value at the specified index, invokes the
        /// callback with the selected value,  and then closes the current context. If an invalid index is provided, the
        /// method will fail silently.</remarks>
        /// <param name="index">The zero-based index of the enumeration value to select. Must be within the bounds of the enumeration values
        /// array.</param>
        private void SetEnumValue(int index)
        {
            InspectorHook.InvokePreProcess();
            _onValueSelected?.Invoke((Enum)_enumValues.GetValue(index));
            InspectorHook.InvokePostProcess();
            Close();
        }
    }
}
#endif