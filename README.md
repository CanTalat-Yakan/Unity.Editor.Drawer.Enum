# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Enum Drawer

> Quick overview: A better enum popup for the Inspector with search, keyboard navigation, and virtualization. Also supports string fields annotated with `[Enum("OptionsSource")]` to pick from a string[] at runtime.

This module replaces the default enum field experience with a searchable popup and smooth keyboard nav. It also adds an attribute-based workflow for string fields, letting you present a dropdown fed by a `string[]` on your component.

![screenshot](Documentation/Screenshot.png)

## Features
- Enhanced enum popup for any enum field
  - Search box (shown automatically for 10+ items)
  - Up/Down/PageUp/PageDown/Enter/Escape keyboard support
  - Virtualized list for large enums for snappy performance
- String dropdowns via attribute
  - `[Enum("OptionsSource")]` on a `string` field renders a dropdown
  - Options are taken from a `string[]` field or property named `OptionsSource` on the same object
- Utility API for custom editors: `EnumDrawer.EnumPopup(...)` overloads
- Editor-only; zero runtime overhead

## Requirements
- Unity Editor 6000.0+ (Editor-only; attribute lives in Runtime for convenience)
- Inspector Hooks module (utilities used by the drawer)
- Editor Window Drawer module (popup window host used by the enum picker)

Tip: If the search popup doesn’t open or keyboard focus isn’t captured, ensure the Inspector Hooks and Editor Window Drawer packages are installed.

## Usage
Basic enum field (no changes needed)

```csharp
public enum Quality { Low, Medium, High }

public class Example : MonoBehaviour
{
    public Quality quality; // Automatically gets the enhanced popup
}
```

String field bound to a string[] options source

```csharp
using UnityEngine;

public class LocaleSelector : MonoBehaviour
{
    // Source options (field or property) — same component
    public string[] Locales = new[] { "en-US", "fr-FR", "de-DE" };

    // Annotate the string field with the source name
    [Enum("Locales")] public string CurrentLocale;
}
```

Programmatic use in custom editors

```csharp
// Inside a custom inspector OnInspectorGUI:
var rect = EditorGUILayout.GetControlRect();
var current = myEnumField; // an Enum value
UnityEssentials.EnumDrawer.EnumPopup(rect, typeof(Quality), current, newValue => myEnumField = (Quality)newValue);
```

## How It Works
- For enum properties
  - The PropertyDrawer detects `SerializedPropertyType.Enum`, resolves the enum type, reads the current value, and shows a popup button
  - Clicking (or pressing Enter/Space when focused) opens a custom dropdown window with search, virtualization, and keyboard navigation
  - On selection, the serialized property is updated; pre/post process hooks run via Inspector Hooks
- For strings with `[Enum("OptionsSource")]`
  - The drawer finds a `string[]` field or property named `OptionsSource` on the target object via reflection
  - It displays a popup of those options and writes the chosen string back to the serialized field
- Public utility: `EnumDrawer.EnumPopup(...)` overloads let other drawers/editors reuse the same popup

## Notes and Limitations
- Attribute scope: `[Enum]` targets `string` fields only; the source must be a `string[]` on the same component (field or property)
- Null/empty options: if the source array is null or empty, the popup shows `<None>`
- Namespacing: the `EnumAttribute` type is defined in the global namespace; use `[Enum("...")]` directly
- Keyboard focus: popup captures Up/Down/PageUp/PageDown/Enter/Escape while active
- Multi-object editing: selection applies per inspected target
- Editor-only: no effect at runtime

## Files in This Package
- `Runtime/EnumAttribute.cs` – `[Enum("OptionsSource")]` attribute (bind a string field to a string[] options source)
- `Editor/EnumDrawer.cs` – PropertyDrawer for enums and for string fields with `[Enum]`; utility `EnumPopup` methods
- `Editor/EnumEditor.cs` – Dropdown window with search, virtualization, keyboard handling
- `Runtime/UnityEssentials.EnumDrawer.asmdef` – Runtime assembly definition
- `Editor/UnityEssentials.EnumDrawer.Editor.asmdef` – Editor assembly definition

## Tags
unity, unity-editor, attribute, propertydrawer, enum, dropdown, searchable, keyboard, virtualization, inspector, ui, tools, workflow
