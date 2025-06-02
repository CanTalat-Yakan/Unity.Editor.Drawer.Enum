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
        /// <summary>
        /// Renders a custom GUI for a serialized property, providing a searchable popup for enum values.
        /// </summary>
        /// <remarks>If the property represents an enum type, a searchable popup is displayed to allow
        /// users to select an enum value. If an error occurs or the property is not an enum, the default property field
        /// is rendered instead.</remarks>
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

        /// <summary>
        /// Determines the underlying enum type associated with the specified serialized property.
        /// </summary>
        /// <remarks>This method is typically used to retrieve the enum type for properties that are
        /// serialized in Unity's editor. It handles cases where the property represents an array or a generic
        /// collection of enums.</remarks>
        /// <param name="property">The serialized property for which to determine the enum type.</param>
        /// <returns>The <see cref="Type"/> representing the enum type of the serialized property.  If the property is an array
        /// or a generic collection, the method returns the element or generic argument type.</returns>
        private Type GetEnumType(SerializedProperty property)
        {
            var enumType = fieldInfo.FieldType;
            if (enumType.IsArray)
                enumType = enumType.GetElementType();
            else if (enumType.IsGenericType)
                enumType = enumType.GetGenericArguments()[0];
            return enumType;
        }

        /// <summary>
        /// Retrieves the current value of an enumeration property as an <see cref="Enum"/> instance.
        /// </summary>
        /// <param name="enumType">The type of the enumeration to which the property belongs. Must be a valid enumeration type.</param>
        /// <param name="property">The serialized property containing the enumeration value. The property must represent an enumeration field.</param>
        /// <returns>An <see cref="Enum"/> instance representing the current value of the enumeration property.</returns>
        private Enum GetCurrentValue(Type enumType, SerializedProperty property) =>
            (Enum)Enum.ToObject(enumType, property.enumValueIndex);

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
                buttonPosition.width -= EditorGUIUtility.labelWidth;
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
            var keyboardClicked = InspectorFocusedHelper.ProcessKeyboardClick(position);
            if (buttonClicked || keyboardClicked)
                EnumSearchPopup.Show(position, enumType, currentValue, onValueChanged);
        }

        /// <summary>
        /// Displays a popup menu for selecting a value from the specified enumeration type.
        /// </summary>
        /// <remarks>This method is typically used in custom editor scripts to render an enumeration
        /// selection popup in the Unity Editor. The selected value is automatically applied to the provided serialized
        /// property.</remarks>
        /// <param name="position">The screen rectangle that defines the position and size of the popup menu.</param>
        /// <param name="currentValue">The currently selected enumeration value to display in the popup.</param>
        /// <param name="enumType">The type of the enumeration to display in the popup. Must be a valid <see cref="System.Enum"/> type.</param>
        /// <param name="property">The serialized property to update with the selected enumeration value.</param>
        public static void EnumPopup(Rect position, Enum currentValue, Type enumType, SerializedProperty property) =>
            EnumPopup(position, currentValue, enumType, (newValue) => SetEnumValue(property, newValue, enumType));

        /// <summary>
        /// Displays a popup menu for selecting a value from an enumeration.
        /// </summary>
        /// <remarks>This method provides a strongly-typed interface for displaying a popup menu for
        /// enumeration values.  The generic type parameter <typeparamref name="T"/> ensures type safety and eliminates
        /// the need for casting.</remarks>
        /// <typeparam name="T">The type of the enumeration. Must be a valid <see langword="enum"/> type.</typeparam>
        /// <param name="position">The screen rectangle that defines the position and size of the popup menu.</param>
        /// <param name="currentValue">The currently selected enumeration value. Determines the initial selection in the popup.</param>
        /// <param name="onValueChanged">A callback action invoked when the user selects a new value from the popup.  The selected value is passed as
        /// a parameter of type <typeparamref name="T"/>.</param>
        public static void EnumPopup<T>(Rect position, Enum currentValue, Action<T> onValueChanged) where T : Enum =>
            EnumPopup(position, currentValue, typeof(T), (newValue) => onValueChanged((T)newValue));

        /// <summary>
        /// Displays a popup menu for selecting a value from an enumeration of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method provides a convenient way to render a dropdown menu for selecting
        /// enumeration values in a graphical user interface. The generic type parameter <typeparamref name="T"/>
        /// ensures type safety by restricting the popup to a specific enumeration type.</remarks>
        /// <typeparam name="T">The enumeration type to display in the popup. Must be a valid <see langword="enum"/> type.</typeparam>
        /// <param name="position">The screen rectangle that defines the position and size of the popup menu.</param>
        /// <param name="currentValue">The currently selected enumeration value. This determines the initial selection in the popup.</param>
        public static void EnumPopup<T>(Rect position, Enum currentValue) where T : Enum =>
            EnumPopup<T>(position, currentValue, (newValue) => currentValue = newValue);

        /// <summary>
        /// Sets the value of a serialized property to the specified enumeration value.
        /// </summary>
        /// <remarks>This method updates the serialized property to reflect the specified enumeration
        /// value.  Ensure that the <paramref name="property"/> represents an enumeration field and that the provided 
        /// <paramref name="newValue"/> is valid for the specified <paramref name="enumType"/>.</remarks>
        /// <param name="property">The serialized property whose value is to be set. This property must represent an enumeration.</param>
        /// <param name="newValue">The new enumeration value to assign to the property.</param>
        /// <param name="enumType">The type of the enumeration. This must match the type of the serialized property.</param>
        public static void SetEnumValue(SerializedProperty property, Enum newValue, Type enumType)
        {
            var newIndex = Array.IndexOf(Enum.GetValues(enumType), newValue);
            SetEnumValue(property, newIndex);
        }

        /// <summary>
        /// Sets the value of an enum property to the specified index.
        /// </summary>
        /// <remarks>If the specified <paramref name="property"/> is <see langword="null"/> or the current
        /// enum value index is already equal to <paramref name="newIndex"/>, no changes are made. After setting the new
        /// index, the method applies the modified properties to the serialized object.</remarks>
        /// <param name="property">The <see cref="SerializedProperty"/> representing the enum field. Must not be <see langword="null"/>.</param>
        /// <param name="newIndex">The new index to set for the enum value. Must be a valid index for the enum.</param>
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