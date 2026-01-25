using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy), true)]
[CanEditMultipleObjects]
public class EnemyEditor : Editor
{
    private bool showAllSettings = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Add some spacing
        EditorGUILayout.Space(5);

        // All Settings
        showAllSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAllSettings, "Enemy Settings");
        if (showAllSettings)
        {
            EditorGUI.indentLevel++;

            // Detection Settings
            EditorGUILayout.LabelField("Detection Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("detectionRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("detectionAngle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleLayers"));
            EditorGUILayout.Space(3);

            // Movement Settings
            EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("waypointReachedDistance"));
            EditorGUILayout.Space(3);

            // Pathfinding Settings
            EditorGUILayout.LabelField("Pathfinding Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pathUpdateInterval"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("usePathfinding"));
            EditorGUILayout.Space(3);

            // Random Walking Settings
            EditorGUILayout.LabelField("Random Walking Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomWalkSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomWalkRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleTimeMin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleTimeMax"));
            EditorGUILayout.Space(3);

            // Health Settings
            EditorGUILayout.LabelField("Health Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHealth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deathAnimationDuration"));
            EditorGUILayout.Space(3);

            // AI Settings
            EditorGUILayout.LabelField("AI Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isAIEnabled"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerLostTimeout"));
            EditorGUILayout.Space(3);

            // References
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("detectionTransform"));
            EditorGUILayout.Space(3);

            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isDebugging"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(5);

        // Draw derived class properties (for Magic_Enemy, Spray_Imp, etc.)
        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "detectionRange",
            "detectionAngle",
            "obstacleLayers",
            "moveSpeed",
            "rotationSpeed",
            "stopDistance",
            "waypointReachedDistance",
            "pathUpdateInterval",
            "usePathfinding",
            "randomWalkSpeed",
            "randomWalkRadius",
            "idleTimeMin",
            "idleTimeMax",
            "maxHealth",
            "deathAnimationDuration",
            "isAIEnabled",
            "playerLostTimeout",
            "detectionTransform",
            "isDebugging"
        );

        serializedObject.ApplyModifiedProperties();
    }
}