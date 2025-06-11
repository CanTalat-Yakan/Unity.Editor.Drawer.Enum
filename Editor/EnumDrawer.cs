#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a custom property drawer for enumerations in the Unity Editor, enabling enhanced functionality such as
    /// search popups.
    /// </summary>
    /// <remarks>This class extends <see cref="PropertyDrawer"/> to customize the rendering and interaction of
    /// enumeration fields in the Unity Inspector. It supports features like displaying a searchable popup for selecting
    /// enumeration values, improving usability for enums with many options.</remarks>
    [CustomPropertyDrawer(typeof(Enum), true)]
    public class EnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                var enumType = InspectorHookUtilities.GetEnumType(fieldInfo);
                var currentValue = InspectorHookUtilities.GetCurrentValue(enumType, property);
                string[] enumNames = Enum.GetNames(enumType);

                DrawSearchPopup(position, property, label, enumType, currentValue, enumNames);
            }
            catch { EditorGUI.PropertyField(position, property, label); }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Displays a popup UI element for selecting an enumeration value.
        /// </summary>
        /// <remarks>This method renders a labeled popup control in the Unity Editor, allowing users to
        /// select a value from the specified enumeration. The label is optional and will only be displayed if <paramref
        /// name="label"/> is not <see cref="GUIContent.none"/>.</remarks>
        /// <param name="position">The screen position and size of the popup control.</param>
        /// <param name="property">The serialized property associated with the selected enumeration value.</param>
        /// <param name="label">The label to display next to the popup control. Use <see cref="GUIContent.none"/> to omit the label.</param>
        /// <param name="enumType">The type of the enumeration to display in the popup.</param>
        /// <param name="currentValue">The currently selected enumeration value.</param>
        /// <param name="enumNames">An array of enumeration names to display in the popup.</param>
        private void DrawSearchPopup(Rect position, SerializedProperty property, GUIContent label, Type enumType, Enum currentValue, string[] enumNames)
        {
            var labelPosition = new Rect(position);
            var buttonPosition = new Rect(position);

            if (label != GUIContent.none)
            {
                labelPosition.width = EditorGUIUtility.labelWidth;
                buttonPosition.x += EditorGUIUtility.labelWidth + 2;
                buttonPosition.width -= EditorGUIUtility.labelWidth + 2;
                EditorGUI.LabelField(labelPosition, label);
            }

            EnumPopup(buttonPosition, currentValue, enumType, property);
        }

        /// <summary>
        /// Displays a popup menu for selecting a value from an enumeration.
        /// </summary>
        /// <remarks>This method creates a popup menu styled for use in the Unity Editor. It allows users
        /// to select a value from the specified enumeration. The popup is triggered by a button click or a keyboard
        /// interaction within the specified rectangle.</remarks>
        /// <param name="position">The screen rectangle that defines the position and size of the popup menu.</param>
        /// <param name="currentValue">The currently selected enumeration value. This determines the initial selection in the popup.</param>
        /// <param name="enumType">The type of the enumeration to display in the popup. Must be a valid <see cref="System.Enum"/> type.</param>
        /// <param name="onValueChanged">A callback action that is invoked when the user selects a new value from the popup. The selected value is
        /// passed as a parameter to the callback.</param>
        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, Action<Enum> onValueChanged)
        {
            var buttonText = ObjectNames.NicifyVariableName(currentValue.ToString());

            var buttonClicked = GUI.Button(position, new GUIContent(buttonText), EditorStyles.popup);
            var keyboardClicked = InspectorFocusedHelper.ProcessKeyboardClick(position, out var controlID);
            if (buttonClicked || keyboardClicked)
                EnumSearchPopup.Show(position, enumType, currentValue, onValueChanged);

            if (InspectorFocusedHelper.IsControlFocused(controlID)) 
                HandleKeyboardInput(position, currentValue, enumType, onValueChanged);
        }

        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, SerializedProperty property) =>
            EnumPopup(position, currentValue, enumType, (newValue) => InspectorHookUtilities.SetEnumValue(property, newValue));

        public static void EnumPopup<T>(Rect position, Enum currentValue, Action<T> onValueChanged) where T : Enum =>
            EnumPopup(position, currentValue, typeof(T), (newValue) => onValueChanged((T)newValue));

        public static void EnumPopup<T>(Rect position, Enum currentValue) where T : Enum =>
            EnumPopup<T>(position, currentValue, (newValue) => currentValue = newValue);

        private static void HandleKeyboardInput(Rect position, Enum currentValue, Type enumType, Action<Enum> onValueChanged)
        {
            // Keyboard navigation for focused enum field (when popup is NOT open)
            if (Event.current.type == EventType.KeyDown)
            {
                var enumValues = Enum.GetValues(enumType);
                int currentIndex = Array.IndexOf(enumValues, currentValue);
                int newIndex = currentIndex;

                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    newIndex = Mathf.Min(currentIndex + 1, enumValues.Length - 1);
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    newIndex = Mathf.Max(currentIndex - 1, 0);
                    Event.current.Use();
                }

                if (newIndex != currentIndex)
                {
                    onValueChanged((Enum)enumValues.GetValue(newIndex));

                }
            }
        }
    }
}
#endif