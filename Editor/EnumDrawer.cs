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

            if (EditorGUI.DropdownButton(buttonPosition, new GUIContent(currentValue.ToString()), FocusType.Keyboard))
                EnumSearchPopup.Show(buttonPosition, property.serializedObject.targetObject, property.propertyPath, enumType, currentValue, enumNames);
        }
    }
}
#endif