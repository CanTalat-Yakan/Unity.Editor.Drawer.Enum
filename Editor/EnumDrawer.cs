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

                DrawEnumPopup(position, property, label, enumType, currentValue, enumNames);
            }
            catch { EditorGUI.PropertyField(position, property, label); }

            EditorGUI.EndProperty();
        }

        private void DrawEnumPopup(Rect position, SerializedProperty property, GUIContent label, Type enumType, Enum currentValue, string[] enumNames)
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

        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, Action<Enum> onValueChanged)
        {
            var buttonText = ObjectNames.NicifyVariableName(currentValue.ToString());

            var buttonClicked = GUI.Button(position, new GUIContent(buttonText), EditorStyles.popup);
            var keyboardClicked = InspectorFocusHelper.ProcessKeyboardClick(position, out var controlID);
            if (buttonClicked || keyboardClicked)
            {
                EnumEditor.ShowAsDropDown(position, enumType, currentValue, onValueChanged);
                InspectorFocusHelper.SetControlFocused(controlID);
            }

            if (InspectorFocusHelper.IsControlFocused(controlID)) 
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