using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HelpButtonAttribute))]
public class HelpButtonDrawer : PropertyDrawer
{
    // MARK: Private
    private const float ButtonWidth = 20f;
    private const float Spacing = 2f;
    private const float HelpBoxMargin = 8f;

    // MARK: Lifecycle
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUI.GetPropertyHeight(property, label, true);
        if (!IsOpen(property))
        {
            return height;
        }

        return height + Spacing + GetHelpHeight();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string text = ((HelpButtonAttribute)attribute).text;
        float fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);

        Rect fieldRect = new Rect(position.x, position.y, position.width - ButtonWidth, fieldHeight);
        Rect buttonRect = new Rect(
            position.xMax - ButtonWidth,
            position.y,
            ButtonWidth,
            EditorGUIUtility.singleLineHeight
        );

        EditorGUI.PropertyField(fieldRect, property, new GUIContent(label.text, text), true);

        bool open = IsOpen(property);
        bool toggled = GUI.Toggle(buttonRect, open, "?", EditorStyles.miniButton);
        if (toggled != open)
        {
            SessionState.SetBool(GetKey(property), toggled);
            open = toggled;
        }

        if (open)
        {
            Rect helpRect = new Rect(
                position.x,
                position.y + fieldHeight + Spacing,
                position.width,
                GetHelpHeight()
            );
            EditorGUI.HelpBox(helpRect, text, MessageType.Info);
        }
    }

    // MARK: Private
    private float GetHelpHeight()
    {
        string text = ((HelpButtonAttribute)attribute).text;
        float width = EditorGUIUtility.currentViewWidth - HelpBoxMargin * 4f;
        float height = EditorStyles.helpBox.CalcHeight(new GUIContent(text), width);
        return Mathf.Max(height, EditorGUIUtility.singleLineHeight * 2f);
    }

    private static bool IsOpen(SerializedProperty property)
    {
        return SessionState.GetBool(GetKey(property), false);
    }

    private static string GetKey(SerializedProperty property)
    {
        return "HelpButton." + property.serializedObject.targetObject.GetHashCode() + "." + property.propertyPath;
    }
}
