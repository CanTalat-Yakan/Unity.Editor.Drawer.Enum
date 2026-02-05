#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides functionality for displaying and interacting with a dropdown editor for selecting values from an
    /// enumeration.
    /// </summary>
    /// <remarks>The <see cref="EnumEditor"/> class allows users to select a value from an enumeration using a
    /// dropdown interface. It supports features such as search filtering, keyboard navigation, and virtualization for
    /// large enumerations.</remarks>
    public class EnumEditor
    {
        public EditorWindowBuilder Window;
        public Action Repaint;
        public Action Close;

        private readonly string[] _names;
        private readonly Array _values;
        private readonly int _currentIndex;
        private string _currentSearchString = string.Empty;
        private int _hoverIndex = -1;

        // Callbacks for each mode
        private readonly Action<Enum> _onEnumSelected;
        private readonly Action<int, string> _onStringSelected;

        private readonly bool _isEnumMode;

        private const float LineHeight = 22f;
        private const int ShowSearchFieldThreshold = 10;
        private const int VirtualizationPadding = 2;

        private string[] _enumNamesLower;
        private string[] _enumNamesNicified;
        private string[] _enumNamesNicifiedLower;

        private List<int> _filteredIndices;
        private bool _needsFilterRefresh = true;
        private int _lastVisibleIndex = -1;
        private int _firstVisibleIndex = -1;
        private bool _hasInitialized;

        // Enum constructor
        public EnumEditor(string[] enumNames, Array enumValues, Enum currentValue, Action<Enum> onValueSelected)
        {
            _isEnumMode = true;
            _names = enumNames;
            _values = enumValues;
            _currentIndex = Array.IndexOf(_values, currentValue);
            _hoverIndex = _currentIndex;
            _onEnumSelected = onValueSelected;

            InitializeNames();
        }

        // String[] constructor
        public EnumEditor(string[] options, int currentIndex, Action<int, string> onValueSelected)
        {
            _isEnumMode = false;
            _names = options;
            _values = null;
            _currentIndex = currentIndex;
            _hoverIndex = _currentIndex;
            _onStringSelected = onValueSelected;

            InitializeNames();
        }

        // Shared initialization logic
        private void InitializeNames()
        {
            int length = _names.Length;

            _enumNamesLower = new string[length];
            _enumNamesNicified = new string[length];
            _enumNamesNicifiedLower = new string[length];

            for (int i = 0; i < length; i++)
            {
                _enumNamesLower[i] = _names[i].ToLowerInvariant();
                _enumNamesNicified[i] = ObjectNames.NicifyVariableName(_names[i]);
                _enumNamesNicifiedLower[i] = _enumNamesNicified[i].ToLowerInvariant();
            }

            RefreshFilteredIndices();
        }

        // Overload for enums
        public static void ShowAsDropDown(Rect buttonPosition, Type enumType, Enum currentValue,
            Action<Enum> onValueSelected)
        {
            var editor = new EnumEditor(Enum.GetNames(enumType), Enum.GetValues(enumType), currentValue,
                onValueSelected);
            ShowAsDropDownInternal(buttonPosition, editor);
        }

        // Overload for string[]
        public static void ShowAsDropDown(Rect buttonPosition, string[] options, int currentIndex,
            Action<int, string> onValueSelected)
        {
            var editor = new EnumEditor(options, currentIndex, onValueSelected);
            ShowAsDropDownInternal(buttonPosition, editor);
        }

        // Internal implementation
        private static void ShowAsDropDownInternal(Rect buttonPosition, EnumEditor editor)
        {
            editor._ignoreFirstKeyDown = true;

            var windowPosition = GUIUtility.GUIToScreenPoint(buttonPosition.position + new Vector2(0, buttonPosition.height));
            windowPosition.y += 42; // Adjust for Unity's window title bar height
            var availableHeight = Screen.currentResolution.height - windowPosition.y;
            var contentWidth = Mathf.Max(buttonPosition.width, 75);
            var searchFieldHeight = editor._isSearchFieldVisible ? LineHeight : 0;
            var maxVisibleItems = Mathf.FloorToInt((availableHeight - searchFieldHeight) / LineHeight);
            var visibleItemCount = Mathf.Min(editor._filteredIndices.Count, maxVisibleItems);
            var contentHeight = searchFieldHeight + (visibleItemCount * LineHeight) + 2;

            editor.Window = ScriptableObject.CreateInstance<EditorWindowBuilder>()
                .AddUpdate(editor.Update)
                .SetPreProcess(editor.PreProcess)
                .SetHeader(editor.Header)
                .SetBody(editor.Body)
                .GetRepaintEvent(out editor.Repaint)
                .GetCloseEvent(out editor.Close)
                .SetDrawBorder()
                .ShowAsDropDown(buttonPosition, new(contentWidth, contentHeight));

            editor.ScrollToCurrentItem();
        }

        public void Update() =>
            HandleMouseMovement();

        public void PreProcess() =>
            HandleKeyboardInput();

        private bool _isSearchFieldVisible => _names.Length >= ShowSearchFieldThreshold;

        public void Header()
        {
            if (!_isSearchFieldVisible)
                return;

            GUI.SetNextControlName("EnumSearchField");
            var newSearch = EditorGUILayout.TextField(_currentSearchString, EditorStyles.toolbarSearchField);
            if (newSearch != _currentSearchString)
            {
                _currentSearchString = newSearch;
                _needsFilterRefresh = true;
            }

            if (!_hasInitialized)
            {
                EditorGUI.FocusTextInControl("EnumSearchField");
                _hasInitialized = true;
            }
        }

        Rect _visibleBody = default;

        public void Body()
        {
            if (_needsFilterRefresh)
                RefreshFilteredIndices();

            // Reserve space for the full list to ensure scrollbar appears
            float totalHeight = _filteredIndices.Count * LineHeight;
            _visibleBody = GUILayoutUtility.GetRect(1, totalHeight, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(false));

            // Calculate visible indices
            _firstVisibleIndex = Mathf.FloorToInt(Window.BodyScrollPosition.y / LineHeight) - VirtualizationPadding;
            _lastVisibleIndex = Mathf.CeilToInt((Window.BodyScrollPosition.y + _visibleBody.height) / LineHeight) +
                                VirtualizationPadding;
            _firstVisibleIndex = Mathf.Clamp(_firstVisibleIndex, 0, _filteredIndices.Count - 1);
            _lastVisibleIndex = Mathf.Clamp(_lastVisibleIndex, 0, _filteredIndices.Count - 1);

            // Prevent out-of-range access if there are no items
            if (_filteredIndices.Count == 0 || _firstVisibleIndex > _lastVisibleIndex)
                return;

            // Only draw items within the visible range
            for (int i = _firstVisibleIndex; i <= _lastVisibleIndex; i++)
            {
                var index = _filteredIndices[i];
                var position = new Rect(_visibleBody.x, _visibleBody.y + i * LineHeight, _visibleBody.width,
                    LineHeight);

                if (Event.current.type == EventType.Repaint)
                {
                    bool isSelected = index == _currentIndex;
                    bool isHovered = index == _hoverIndex;
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

        private void SetEnumValue(int index)
        {
            InspectorHook.InvokePreProcess();
            if (_isEnumMode) _onEnumSelected?.Invoke((Enum)_values.GetValue(index));
            else _onStringSelected?.Invoke(index, _names[index]);
            InspectorHook.InvokePostProcess();
            Close();
        }

        private bool _ignoreFirstKeyDown = true;

        private void HandleKeyboardInput()
        {
            if (_ignoreFirstKeyDown)
                if (Event.current.type == EventType.KeyUp)
                {
                    _ignoreFirstKeyDown = false;
                    Event.current.Use();
                    return;
                }

            if (Event.current.type != EventType.KeyDown)
                return;

            if (_filteredIndices.Count == 0)
                return;

            int current = _filteredIndices.IndexOf(_hoverIndex);
            if (current == -1)
                current = 0;

            switch (Event.current.keyCode)
            {
                case KeyCode.DownArrow:
                    current = (current + 1) % _filteredIndices.Count;
                    _hoverIndex = _filteredIndices[current];
                    EnsureItemVisible(_hoverIndex);
                    Event.current.Use();
                    break;

                case KeyCode.UpArrow:
                    current = (current - 1 + _filteredIndices.Count) % _filteredIndices.Count;
                    _hoverIndex = _filteredIndices[current];
                    EnsureItemVisible(_hoverIndex);
                    Event.current.Use();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SetEnumValue(_hoverIndex);
                    Close();
                    Event.current.Use();
                    break;

                case KeyCode.Escape:
                    Close();
                    Event.current.Use();
                    break;

                case KeyCode.PageDown:
                    current = Mathf.Min(current + 10, _filteredIndices.Count - 1);
                    _hoverIndex = _filteredIndices[current];
                    EnsureItemVisible(_hoverIndex);
                    Event.current.Use();
                    break;

                case KeyCode.PageUp:
                    current = Mathf.Max(current - 10, 0);
                    _hoverIndex = _filteredIndices[current];
                    EnsureItemVisible(_hoverIndex);
                    Event.current.Use();
                    break;
            }
        }

        Vector2 _previousMousePosition = Vector2.zero;

        private void HandleMouseMovement()
        {
            var mousePosition = Window.GetLocalMousePosition();
            if (mousePosition != _previousMousePosition)
                _previousMousePosition = mousePosition;
            else return;

            var visibleBody = _visibleBody;
            visibleBody.y += _isSearchFieldVisible ? LineHeight : 0;

            if (!visibleBody.Contains(mousePosition))
                return;

            var searchFieldHeight = _isSearchFieldVisible ? LineHeight : 0;
            var relativeY = mousePosition.y - searchFieldHeight + Window.BodyScrollPosition.y;
            var newHoverIndex = Mathf.FloorToInt(relativeY / LineHeight);
            if (newHoverIndex >= 0 && newHoverIndex < _filteredIndices.Count)
            {
                var actualIndex = _filteredIndices[newHoverIndex];
                if (_hoverIndex != actualIndex)
                {
                    _hoverIndex = actualIndex;
                    Repaint();
                }
            }
        }

        private void RefreshFilteredIndices()
        {
            _needsFilterRefresh = false;

            if (string.IsNullOrEmpty(_currentSearchString))
            {
                if (_filteredIndices == null || _filteredIndices.Count != _names.Length)
                {
                    _filteredIndices = new(_names.Length);
                    for (int i = 0; i < _names.Length; i++)
                        _filteredIndices.Add(i);
                }

                return;
            }

            string lowerSearch = _currentSearchString.ToLowerInvariant();
            _filteredIndices = new();
            for (int i = 0; i < _names.Length; i++)
                if (_enumNamesLower[i].Contains(lowerSearch) || _enumNamesNicifiedLower[i].Contains(lowerSearch))
                    _filteredIndices.Add(i);

            _hoverIndex = _filteredIndices.Count > 0 ? _filteredIndices[0] : -1;
        }

        private void ScrollToCurrentItem()
        {
            if (!_filteredIndices.Contains(_currentIndex))
                return;

            _hoverIndex = _currentIndex;
            EnsureItemVisible(_currentIndex);
        }

        private void EnsureItemVisible(int enumIndex)
        {
            if (!_filteredIndices.Contains(enumIndex))
                return;

            int indexInList = _filteredIndices.IndexOf(enumIndex);
            float itemPosition = indexInList * LineHeight;
            float itemHeight = LineHeight;
            float viewHeight = Window.Position.height - (_isSearchFieldVisible ? LineHeight : 0);

            if (itemPosition < Window.BodyScrollPosition.y)
                Window.BodyScrollPosition = new Vector2(Window.BodyScrollPosition.x, itemPosition);
            else if (itemPosition + itemHeight > Window.BodyScrollPosition.y + viewHeight)
                Window.BodyScrollPosition =
                    new Vector2(Window.BodyScrollPosition.x, itemPosition - viewHeight + itemHeight);
        }

        private void DrawItemBackground(Rect position, bool highlighted)
        {
            if (!highlighted)
                return;

            EditorGUI.DrawRect(position, EditorWindowBuilder.HighlightColor);
        }

        private void DrawItemText(Rect position, string text, bool highlighted)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0)
            };
            style.normal.textColor = highlighted ? Color.white : EditorStyles.label.normal.textColor;
            style.hover.textColor = highlighted ? Color.white : EditorStyles.label.normal.textColor;

            GUI.Label(position, text, style);
        }
    }
}
#endif