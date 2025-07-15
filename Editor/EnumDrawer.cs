#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Provides a custom property drawer for enumerations in the Unity Editor, enabling enhanced functionality such as
    /// search popups.
    /// </summary>
    /// <remarks>This class extends <see cref="PropertyDrawer"/> to customize the rendering and interaction of
    /// enumeration fields in the Unity Inspector. It supports features like displaying a searchable popup for selecting
    /// enumeration values, improving usability for enums with many options.</remarks>
    [CustomPropertyDrawer(typeof(Enum), true), CustomPropertyDrawer(typeof(string))]
    public class EnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                if (property.propertyType == SerializedPropertyType.Enum)
                {
                    var enumType = InspectorHookUtilities.GetEnumType(fieldInfo);
                    var currentValue = InspectorHookUtilities.GetCurrentValue(enumType, property);
                    DrawEnumPopup(position, property, label, enumType, currentValue);
                }
                else if (property.propertyType == SerializedPropertyType.String && fieldInfo.GetCustomAttribute<EnumAttribute>() != null)
                    DrawStringEnumPopup(position, property, label);
                else EditorGUI.PropertyField(position, property, label, true);
            }
            catch { EditorGUI.PropertyField(position, property, label, true); }

            EditorGUI.EndProperty();
        }

        private void DrawEnumPopup(Rect position, SerializedProperty property, GUIContent label, Type enumType, Enum currentValue)
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

        private void DrawStringEnumPopup(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get options from attribute or field
            string[] options = GetStringOptions(fieldInfo, property);

            // Find current index
            int currentIndex = Array.IndexOf(options, property.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            var labelPosition = new Rect(position);
            var buttonPosition = new Rect(position);

            if (label != GUIContent.none)
            {
                labelPosition.width = EditorGUIUtility.labelWidth;
                buttonPosition.x += EditorGUIUtility.labelWidth + 2;
                buttonPosition.width -= EditorGUIUtility.labelWidth + 2;
                EditorGUI.LabelField(labelPosition, label);
            }

            string buttonText = options.Length > 0 && currentIndex >= 0 && currentIndex < options.Length
                ? ObjectNames.NicifyVariableName(options[currentIndex])
                : "<None>";

            bool buttonClicked = GUI.Button(buttonPosition, new GUIContent(buttonText), EditorStyles.popup);
            if (buttonClicked)
            {
                EnumEditor.ShowAsDropDown(
                    buttonPosition,
                    options,
                    currentIndex,
                    (selectedIndex, selectedValue) =>
                    {
                        property.stringValue = selectedValue;
                        property.serializedObject.ApplyModifiedProperties();
                    });
            }
        }

        /// <summary>
        /// Gets the string options for the dropdown. You can extend this to support options from the attribute, a static method, etc.
        /// </summary>
        private string[] GetStringOptions(FieldInfo fieldInfo, SerializedProperty property)
        {
            // Get the EnumAttribute and its reference name
            var enumAttr = (EnumAttribute)fieldInfo.GetCustomAttribute(typeof(EnumAttribute), true);
            if (enumAttr == null || string.IsNullOrEmpty(enumAttr.ReferenceName))
                return null;

            // Get the target object (the MonoBehaviour/ScriptableObject)
            var target = property.serializedObject.targetObject;
            var targetType = target.GetType();

            // Find the field or property with the given name
            var refField = targetType.GetField(enumAttr.ReferenceName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (refField != null && refField.FieldType == typeof(string[]))
                return refField.GetValue(target) as string[];

            var refProp = targetType.GetProperty(enumAttr.ReferenceName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (refProp != null && refProp.PropertyType == typeof(string[]))
                return refProp.GetValue(target) as string[];

            return null;
        }

        public static void EnumPopup(Rect position, Type enumType, Enum currentValue, Action<Enum> onValueChanged)
        {
            var buttonText = ObjectNames.NicifyVariableName(currentValue.ToString());

            bool buttonClicked = GUI.Button(position, new GUIContent(buttonText), EditorStyles.popup);
            bool keyboardClicked = InspectorFocusHelper.ProcessKeyboardClick(position, out var controlID);
            if (buttonClicked || keyboardClicked)
            {
                EnumEditor.ShowAsDropDown(position, enumType, currentValue, onValueChanged);
                InspectorFocusHelper.SetControlFocused(controlID);
            }

            if (InspectorFocusHelper.IsControlFocused(controlID))
                HandleKeyboardInput(position, enumType, currentValue, onValueChanged);
        }

        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, SerializedProperty property) =>
            EnumPopup(position, enumType, currentValue, (newValue) => InspectorHookUtilities.SetEnumValue(property, newValue));

        public static void EnumPopup<T>(Rect position, Enum currentValue, Action<T> onValueChanged) where T : Enum =>
            EnumPopup(position, typeof(T), currentValue, (newValue) => onValueChanged((T)newValue));

        public static void EnumPopup<T>(Rect position, Enum currentValue) where T : Enum =>
            EnumPopup<T>(position, currentValue, (newValue) => currentValue = newValue);

        private static void HandleKeyboardInput(Rect position, Type enumType, Enum currentValue, Action<Enum> onValueChanged)
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