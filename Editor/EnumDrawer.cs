#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

namespace UnityEssentials
{
    [CustomPropertyDrawer(typeof(Enum), true)]
    public class EnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            try
            {
                var enumType = GetEnumType(property);
                var currentValue = GetCurrentValue(enumType, property);
                string[] enumNames = Enum.GetNames(enumType);

                DrawSearchPopup(position, property, label, enumType, currentValue, enumNames);
            }
            catch { EditorGUI.PropertyField(position, property, label); }

            EditorGUI.EndProperty();
        }

        private Type GetEnumType(SerializedProperty property)
        {
            Type enumType = fieldInfo.FieldType;
            if (enumType.IsArray)
                enumType = enumType.GetElementType();
            else if (enumType.IsGenericType)
                enumType = enumType.GetGenericArguments()[0];
            return enumType;
        }

        private Enum GetCurrentValue(Type enumType, SerializedProperty property) =>
            (Enum)Enum.ToObject(enumType, property.enumValueIndex);

        private void DrawSearchPopup(Rect position, SerializedProperty property, GUIContent label, Type enumType, Enum currentValue, string[] enumNames)
        {
            var labelPosition = new Rect(position);
            var buttonPosition = new Rect(position);

            if (label != GUIContent.none)
            {
                labelPosition.width = EditorGUIUtility.labelWidth;
                buttonPosition.x += EditorGUIUtility.labelWidth;
                buttonPosition.width -= EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelPosition, label);
            }

            EnumPopup(buttonPosition, currentValue, enumType, property);
        }

        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, Action<Enum> onValueChanged)
        {
            var buttonText = ObjectNames.NicifyVariableName(currentValue.ToString());
            var buttonClicked = GUI.Button(position, new GUIContent(buttonText), EditorStyles.popup);
            var keyboardClicked = InspectorFocusedHelper.ProcessKeyboardClick(position);
            if (buttonClicked || keyboardClicked)
                EnumSearchPopup.Show(position, enumType, currentValue, onValueChanged);
        }

        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, SerializedProperty property) =>
            EnumPopup(position, currentValue, enumType, (newValue) => SetEnumValue(property, newValue, enumType));

        public static void EnumPopup<T>(Rect position, Enum currentValue, Action<T> onValueChanged) where T : Enum =>
            EnumPopup(position, currentValue, typeof(T), (newValue) => onValueChanged((T)newValue));

        public static void EnumPopup<T>(Rect position, Enum currentValue) where T : Enum =>
            EnumPopup<T>(position, currentValue, (newValue) => currentValue = newValue);

        public static void SetEnumValue(SerializedProperty property, Enum newValue, Type enumType)
        {
            var newIndex = Array.IndexOf(Enum.GetValues(enumType), newValue);
            SetEnumValue(property, newIndex);
        }

        public static void SetEnumValue(SerializedProperty property, int newIndex)
        {
            if (property != null && property.enumValueIndex != newIndex)
            {
                property.enumValueIndex = newIndex;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif