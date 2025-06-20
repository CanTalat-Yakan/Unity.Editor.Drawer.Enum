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
        public EditorWindowDrawer Window;
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

        private string[] _enumNamesLower;
        private string[] _enumNamesNicified;
        private string[] _enumNamesNicifiedLower;

        public EnumEditor(Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            _enumValues = Enum.GetValues(enumType);
            _enumNames = Enum.GetNames(enumType);
            _currentIndex = Array.IndexOf(_enumValues, currentValue);
            _hoverIndex = _currentIndex;
            _onValueSelected = onValueSelected;

            int length = _enumNames.Length;
            _enumNamesLower = new string[length];
            _enumNamesNicified = new string[length];
            _enumNamesNicifiedLower = new string[length];
            for (int i = 0; i < length; i++)
            {
                _enumNamesLower[i] = _enumNames[i].ToLowerInvariant();
                _enumNamesNicified[i] = ObjectNames.NicifyVariableName(_enumNames[i]);
                _enumNamesNicifiedLower[i] = _enumNamesNicified[i].ToLowerInvariant();
            }
        }

        public static void ShowAsDropDown(Rect buttonPosition, Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            var editor = new EnumEditor(enumType, currentValue, onValueSelected);

            var windowPosition = GUIUtility.GUIToScreenPoint(buttonPosition.position + new Vector2(0, buttonPosition.height));
            var availableHeight = Screen.currentResolution.height - windowPosition.y;
            var contentwidth = Mathf.Max(MinWindowWidth, buttonPosition.width);
            var calculateContentHeight = LineHeight + (editor.GetFilteredIndices().Count * LineHeight) + 1;
            var contentHeight = Mathf.Min(MaxWindowHeight, availableHeight, calculateContentHeight);
            var dropdownSize = new Vector2(contentwidth, contentHeight - 3);

            editor.Window = new EditorWindowDrawer()
                .AddUpdate(editor.Update)
                .SetPreProcess(editor.PreProcess)
                .SetPostProcess(editor.PostProcess)
                .SetHeader(editor.Header)
                .SetBody(editor.Body)
                .GetRepaintEvent(out editor.Repaint)
                .GetCloseEvent(out editor.Close)
                .SetDrawBorder()
                .ShowAsDropDown(buttonPosition, dropdownSize);

            editor.ScrollToCurrentItem();
        }

        private void Update() =>
            HandleMouseMovement();

        public void PreProcess() =>
            HandleKeyboardInput();

        public void PostProcess()
        {
            if (_currentSearchString != _previousSearchString)
            {
                _previousSearchString = _currentSearchString;
                var filtered = GetFilteredIndices();
                _hoverIndex = filtered.Count > 0 ? filtered[0] : -1;
                if (_hoverIndex != -1)
                    ScrollToItem(_hoverIndex);
            }
        }

        public void Header()
        {
            GUI.SetNextControlName("SearchField");
            _currentSearchString = GUILayout.TextField(_currentSearchString, EditorStyles.toolbarSearchField);
            EditorGUI.FocusTextInControl("SearchField");
        }

        public void Body()
        {
            var filteredIndices = GetFilteredIndices();
            var listHeight = filteredIndices.Count * LineHeight;

            for (int i = 0; i < filteredIndices.Count; i++)
            {
                var index = filteredIndices[i];
                var position = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(LineHeight));

                if (Event.current.type == EventType.Repaint)
                {
                    var isSelected = index == _currentIndex;
                    var isHovered = index == _hoverIndex;
                    DrawItemBackground(position, isSelected || isHovered);
                    DrawItemText(position, _enumNamesNicified[index], isSelected || isHovered);
                }

                if (GUI.Button(position, GUIContent.none, GUIStyle.none))
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
            var currentMousePosition = Window.GetLocalMousePosition();
            if (_previousMousePosition != currentMousePosition)
                _previousMousePosition = currentMousePosition;
            else return;

            var contentPosition = new Rect(0, LineHeight, Window.Position.width, Window.Position.height);
            if (contentPosition.Contains(Window.GetLocalMousePosition()))
            {
                var filtered = GetFilteredIndices();
                var scrollY = currentMousePosition.y - LineHeight + Window.ScrollPosition.y;
                var itemIndex = Mathf.FloorToInt(scrollY / LineHeight);
                _hoverIndex = itemIndex >= 0 && itemIndex < filtered.Count ? filtered[itemIndex] : -1;
                Repaint();
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
            var scrollViewHeight = Window.Position.height - LineHeight - 2 * Padding; // Account for search field and padding
            var maxScroll = Mathf.Max(0, (filtered.Count * LineHeight) - scrollViewHeight);

            // Center the item in the scroll view
            Window.ScrollPosition.y = Mathf.Clamp(itemPosition - (scrollViewHeight / 2), 0, maxScroll);
        }

        private static readonly Color s_highlightColorPro = new Color(0.24f, 0.37f, 0.58f);
        private static readonly Color s_highlightColorLight = new Color(0.22f, 0.44f, 0.9f);
        private void DrawItemBackground(Rect position, bool highlighted)
        {
            if (highlighted)
            {
                var color = EditorGUIUtility.isProSkin ? s_highlightColorPro : s_highlightColorLight;
                EditorGUI.DrawRect(position, color);
            }
        }

        private void DrawItemText(Rect position, string text, bool highlighted)
        {
            var color = highlighted ? Color.white : EditorStyles.label.normal.textColor;
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0),
                normal = { textColor = color },
                hover = { textColor = color }
            };

            GUI.Label(position, text, style);
        }

        private List<int> _filteredIndicesCache = null;
        private List<int> GetFilteredIndices()
        {
            if (_filteredIndicesCache != null && _currentSearchString == _previousSearchString)
                return _filteredIndicesCache;

            List<int> indices;
            if (string.IsNullOrEmpty(_currentSearchString))
            {
                indices = new(_enumNames.Length);
                for (int i = 0; i < _enumNames.Length; i++)
                    indices.Add(i);
            }
            else
            {
                string lowerSearch = _currentSearchString.ToLowerInvariant();
                indices = new();
                for (int i = 0; i < _enumNames.Length; i++)
                    if (_enumNamesLower[i].Contains(lowerSearch) || _enumNamesNicifiedLower[i].Contains(lowerSearch))
                        indices.Add(i);
            }

            return _filteredIndicesCache = indices;
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