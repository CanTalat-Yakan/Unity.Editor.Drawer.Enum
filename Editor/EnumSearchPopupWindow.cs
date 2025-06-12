#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a popup window for selecting an enumeration value with a searchable list.
    /// </summary>
    /// <remarks>This class is designed to display a dropdown-style popup for selecting a value from an
    /// enumeration. It supports searching through the enum values and highlights the currently selected or hovered
    /// item. The popup is typically used in custom Unity editor tools to enhance user experience when working with
    /// enums.</remarks>
    public class EnumSearchPopup : EditorWindow
    {
        private const float LineHeight = 22f;
        private const float Padding = 4f;
        private const float MinWindowWidth = 75f;
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

        /// <summary>
        /// Displays a popup window for selecting a value from an enumeration.
        /// </summary>
        /// <remarks>The popup window is displayed as a dropdown below the specified button rectangle. It
        /// dynamically adjusts its size based on the available screen space and the number of enumeration values. The
        /// caller must ensure that <paramref name="enumType"/> is a valid enumeration type; otherwise, the behavior is
        /// undefined.</remarks>
        /// <param name="buttonRect">The screen-space rectangle of the button that triggers the popup. Used to position the popup window.</param>
        /// <param name="enumType">The type of the enumeration to display in the popup. Must be a valid <see cref="System.Enum"/> type.</param>
        /// <param name="currentValue">The currently selected enumeration value. The popup will highlight this value initially.</param>
        /// <param name="onValueSelected">A callback action invoked when a new enumeration value is selected. The selected value is passed as a
        /// parameter to this action.</param>
        public static void Show(Rect buttonRect, Type enumType, Enum currentValue, Action<Enum> onValueSelected)
        {
            var window = CreateInstance<EnumSearchPopup>();
            window._enumValues = Enum.GetValues(enumType);
            window._enumNames = Enum.GetNames(enumType);
            window._currentIndex = Array.IndexOf(window._enumValues, currentValue);
            window._onValueSelected = onValueSelected;
            window._searchString = string.Empty;
            window._hoverIndex = window._currentIndex;

            var windowPosition = GUIUtility.GUIToScreenPoint(buttonRect.position + new Vector2(0, buttonRect.height));
            var availableHeight = Screen.currentResolution.height - windowPosition.y;

            var contentHeight = Mathf.Min(
                window.CalculateContentHeight(),
                MaxWindowHeight,
                availableHeight);

            window.position = new Rect(
                windowPosition.x - Padding,
                windowPosition.y,
                Mathf.Max(buttonRect.width + Padding * 2, MinWindowWidth),
                contentHeight + Padding * 2);

            var windowSize = new Vector2(Mathf.Max(buttonRect.width, MinWindowWidth), contentHeight);
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), windowSize);
            window.Focus();
            window.ScrollToCurrentItem();
        }

        public void OnGUI()
        {
            string previousSearch = _searchString;
            HandleKeyboardInput();
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

        /// <summary>
        /// Handles keyboard input events to navigate and interact with a filtered list of items.
        /// </summary>
        /// <remarks>This method processes key events such as arrow keys for navigation, the Enter key for
        /// selection,  and the Escape key for closing the interface. It updates the hover index, scrolls to the
        /// selected  item, and repaints the UI as necessary. The method only acts on key down events and ignores input 
        /// if the filtered list is empty.</remarks>
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

        /// <summary>
        /// Handles mouse movement events to update the hover index and trigger a repaint if necessary.
        /// </summary>
        /// <remarks>This method processes mouse movement and layout events to determine if the mouse
        /// position has changed. If the mouse is within the content area, it calculates the index of the item being
        /// hovered over and updates the hover index accordingly. A repaint is triggered when the hover index
        /// changes.</remarks>
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

        /// <summary>
        /// Scrolls to the currently selected item in the filtered list, if it exists.
        /// </summary>
        /// <remarks>This method checks whether the current index is part of the filtered indices.  If it
        /// is, the hover index is updated to match the current index, and the view  is scrolled to the corresponding
        /// item.</remarks>
        private void ScrollToCurrentItem()
        {
            var filtered = GetFilteredIndices();
            if (filtered.Contains(_currentIndex))
            {
                _hoverIndex = _currentIndex;
                ScrollToItem(_currentIndex);
            }
        }

        /// <summary>
        /// Scrolls the view to bring the specified item into focus.
        /// </summary>
        /// <remarks>If the specified <paramref name="index"/> is not present in the filtered list, the
        /// method does nothing.  The item is centered in the scroll view if possible, taking into account the view's
        /// height and padding.</remarks>
        /// <param name="index">The zero-based index of the item to scroll to. Must correspond to a valid item in the filtered list.</param>
        private void ScrollToItem(int index)
        {
            var filtered = GetFilteredIndices();
            var itemIndex = filtered.IndexOf(index);
            if (itemIndex == -1)
                return;

            var itemPosition = itemIndex * LineHeight;
            var scrollViewHeight = position.height - LineHeight - 2 * Padding; // Account for search field and padding
            var maxScroll = Mathf.Max(0, (filtered.Count * LineHeight) - scrollViewHeight);

            // Center the item in the scroll view
            _scrollPosition.y = Mathf.Clamp(itemPosition - (scrollViewHeight / 2), 0, maxScroll);
        }

        /// <summary>
        /// Draws a bordered area with a background color and renders the specified content inside it.
        /// </summary>
        /// <remarks>The border and background colors are determined based on the current editor theme
        /// (light or dark mode). The method ensures that the content is drawn within a properly padded area inside the
        /// border.</remarks>
        /// <param name="drawContent">A delegate that defines the content to be drawn inside the bordered area. This action is invoked within the
        /// inner area of the border.</param>
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

        /// <summary>
        /// Displays a scrollable list of enum values, allowing the user to select one.
        /// </summary>
        /// <remarks>This method renders a filtered list of enum values in a scrollable view. Each item in
        /// the list can be selected by clicking on it, which updates the current selection and closes the list. The
        /// appearance of the items changes based on their selection or hover state.</remarks>
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

        /// <summary>
        /// Draws the background of an item within a specified rectangular area.
        /// </summary>
        /// <param name="rect">The rectangular area where the background will be drawn.</param>
        /// <param name="highlighted"><see langword="true"/> to draw the background with a highlighted color; otherwise, the background will not
        /// be drawn.</param>
        private void DrawItemBackground(Rect rect, bool highlighted)
        {
            if (highlighted)
            {
                var color = EditorGUIUtility.isProSkin ? s_highlightColorPro : s_highlightColorLight;
                EditorGUI.DrawRect(rect, color);
            }
        }

        /// <summary>
        /// Draws the specified text within the given rectangular area, applying a visual style that optionally
        /// highlights the text.
        /// </summary>
        /// <remarks>The method uses a custom <see cref="GUIStyle"/> to render the text, with alignment
        /// set to the left and padding applied. If <paramref name="highlighted"/> is <see langword="true"/>, the text
        /// color is set to white.</remarks>
        /// <param name="rect">The rectangular area in which to draw the text.</param>
        /// <param name="text">The text to be displayed. The text will be formatted using <see
        /// cref="ObjectNames.NicifyVariableName(string)"/>.</param>
        /// <param name="highlighted">A value indicating whether the text should be highlighted.  <see langword="true"/> to apply a highlighted
        /// style; otherwise, <see langword="false"/>.</param>
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
        /// Renders a search field in the editor toolbar and sets focus to it.
        /// </summary>
        /// <remarks>This method creates a horizontal toolbar layout containing a text field styled as a
        /// search field. The text field is assigned a control name of "SearchField" to allow programmatic focus
        /// management.</remarks>
        private void DrawSearchField()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("SearchField");
            _searchString = GUILayout.TextField(_searchString, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();
            EditorGUI.FocusTextInControl("SearchField");
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