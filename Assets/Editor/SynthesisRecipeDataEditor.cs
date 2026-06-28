using RPG.MasterData;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SynthesisRecipeData))]
public sealed class SynthesisRecipeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("requiredSynthesisLevel"));

        var productType = serializedObject.FindProperty("productType");
        var productItem = serializedObject.FindProperty("productItem");
        var productEquipment = serializedObject.FindProperty("productEquipment");
        EditorGUILayout.PropertyField(productType);

        if ((SynthesisProductDataType)productType.enumValueIndex == SynthesisProductDataType.Equipment)
        {
            productItem.objectReferenceValue = null;
            EditorGUILayout.PropertyField(productEquipment);
        }
        else
        {
            productEquipment.objectReferenceValue = null;
            EditorGUILayout.PropertyField(productItem);
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("moneyCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("materialCosts"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sortOrder"));

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomPropertyDrawer(typeof(SynthesisMaterialCostData))]
public sealed class SynthesisMaterialCostDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var itemRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var countRect = new Rect(
            position.x,
            itemRect.yMax + EditorGUIUtility.standardVerticalSpacing,
            position.width,
            EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(itemRect, property.FindPropertyRelative("item"));
        EditorGUI.PropertyField(countRect, property.FindPropertyRelative("count"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }
}
